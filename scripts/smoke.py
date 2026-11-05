#!/usr/bin/env python3
"""Prueba de integración: crea usuarios, productos y ventas en una API en ejecución.
Configurar FIRMEZA_API, FIRMEZA_ADMIN_EMAIL y FIRMEZA_ADMIN_PASSWORD.
"""
import datetime
import io
import json
import os
import urllib.error
import urllib.request
import uuid
import zipfile

BASE = os.environ.get('FIRMEZA_API', 'http://localhost:8080')

def request(method, path, data=None, token=None, expected=200, raw=False, content_type=None):
    headers = {}
    if data is not None and not isinstance(data, bytes):
        data = json.dumps(data).encode()
        content_type = 'application/json'
    if content_type:
        headers['Content-Type'] = content_type
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(BASE + path, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=60) as res:
            status, body = res.status, res.read()
    except urllib.error.HTTPError as ex:
        status, body = ex.code, ex.read()
    assert status == expected, f'{method} {path}: esperado {expected}, recibido {status}: {body[:800]!r}'
    return body if raw or not body else json.loads(body)

def main():
    suffix = uuid.uuid4().hex[:12]
    password = 'FirmezaSmoke2026!'
    def register(tag):
        email = f'{tag}-{suffix}@example.com'
        return request('POST','/api/auth/register',{'email':email,'password':password,'customer':{'document':tag+suffix,'name':'Prueba','lastName':'Integración','email':email,'phone':'3001234567','address':'Calle de pruebas 123','age':25}})['token']
    request('GET','/health')
    admin = request('POST','/api/auth/login',{'email':os.environ['FIRMEZA_ADMIN_EMAIL'],'password':os.environ['FIRMEZA_ADMIN_PASSWORD']})['token']
    buyer, other = register('buyer'), register('other')
    product = request('POST','/api/productos',{'name':'Material de prueba '+suffix,'description':'Prueba de integración','price':100,'stock':10},admin,201)
    pid = product['id']
    request('GET','/api/clientes',token=buyer,expected=403)
    request('POST','/api/productos',{'name':'No autorizado','description':'','price':1,'stock':1},buyer,403)
    request('POST','/api/clientes',{'document':'edad'+suffix,'name':'A','lastName':'B','email':f'age-{suffix}@example.com','phone':'3001234567','address':'Calle 1','age':18.5},admin,400)
    sale = request('POST','/api/ventas',{'items':[{'productId':pid,'quantity':2},{'productId':pid,'quantity':1}]},buyer,201)
    assert sale['total'] == 357 and len(sale['details']) == 1
    assert request('GET','/api/productos/'+pid)['stock'] == 7
    pdf = request('GET',f"/api/ventas/{sale['id']}/comprobante",token=buyer,raw=True)
    assert pdf.startswith(b'%PDF')
    request('GET',f"/api/ventas/{sale['id']}/comprobante",token=other,expected=404)
    request('POST','/api/ventas',{'items':[{'productId':pid,'quantity':8}]},buyer,409)
    assert request('GET','/api/productos/'+pid)['stock'] == 7
    request('PUT',f"/api/ventas/{sale['id']}/anular",token=admin,expected=204)
    assert request('GET','/api/productos/'+pid)['stock'] == 10
    request('PUT',f"/api/ventas/{sale['id']}/anular",token=admin,expected=409)
    for resource in ['productos','clientes','ventas']:
        excel=request('GET',f'/api/admin/exportar/{resource}/xlsx',token=admin,raw=True)
        assert zipfile.is_zipfile(io.BytesIO(excel))
        assert request('GET',f'/api/admin/exportar/{resource}/pdf',token=admin,raw=True).startswith(b'%PDF')
    excel=request('GET','/api/admin/exportar/productos/xlsx',token=admin,raw=True)
    boundary='FirmezaBoundary'+suffix
    body=(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="productos.xlsx"\r\nContent-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n').encode()+excel+f'\r\n--{boundary}--\r\n'.encode()
    result=request('POST','/api/admin/importar',body,admin,content_type='multipart/form-data; boundary='+boundary)
    assert not result['errors'] and result['products'] > 0, result
    vehicle=request('POST','/api/vehiculos',{'name':'Camión '+suffix,'type':'Carga','brand':'Prueba','plate':suffix[:10],'pricePerDay':100},admin)
    start=datetime.date.today()+datetime.timedelta(days=2)
    dates={'vehicleId':vehicle['id'],'startDate':str(start),'endDate':str(start+datetime.timedelta(days=2))}
    rental=request('POST','/api/alquileres',dates,buyer)
    assert rental['total'] == 200
    request('POST','/api/alquileres',dates,other,409)
    request('DELETE','/api/alquileres/'+rental['id'],token=buyer,expected=204)
    request('DELETE','/api/productos/'+pid,token=admin,expected=204)
    request('GET','/api/productos/'+pid,expected=404)
    print('OK: registro, roles, validación, venta, stock, PDF, propiedad, anulación, Excel/PDF, importación y alquileres.')

if __name__ == '__main__':
    main()
