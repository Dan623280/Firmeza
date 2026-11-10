import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Auth, CartLine, Product } from './models';
import { totals, validQuantity } from './cart-math.mjs';
import { validSession } from './access.mjs';
@Injectable({providedIn:'root'})
export class Session {
 private router=inject(Router);
 auth=signal<Auth|null>(this.restore());
 private timer:ReturnType<typeof setTimeout>|undefined;
 constructor(){this.schedule();}
 private restore():Auth|null {try{const a=JSON.parse(sessionStorage.getItem('firmeza.session')??'null') as Auth|null;if(validSession(a))return a;sessionStorage.removeItem('firmeza.session');return null;}catch{return null;}}
 private schedule(){if(this.timer)clearTimeout(this.timer);const a=this.auth();if(a)this.timer=setTimeout(()=>this.logout(),Math.max(0,new Date(a.expiresAt).getTime()-Date.now()));}
 save(auth:Auth){if(!validSession(auth))throw new Error('La sesión recibida no es válida.');sessionStorage.setItem('firmeza.session',JSON.stringify(auth));this.auth.set(auth);this.schedule();}
 logout(){sessionStorage.removeItem('firmeza.session');this.auth.set(null);if(this.timer)clearTimeout(this.timer);void this.router.navigate(['/ingresar']);}
}
export const authInterceptor:HttpInterceptorFn=(request,next)=>{
 const session=inject(Session);const auth=session.auth();
 if(auth&&request.url.startsWith('/api/')){if(new Date(auth.expiresAt).getTime()<=Date.now()){session.logout();return throwError(()=>new Error('Tu sesión expiró. Ingresa de nuevo.'));}request=request.clone({setHeaders:{Authorization:`Bearer ${auth.token}`}});}
 return next(request).pipe(catchError((error:HttpErrorResponse)=>{if(error.status===401&&auth)session.logout();return throwError(()=>error);}));
};
@Injectable({providedIn:'root'})
export class Api {http=inject(HttpClient);get<T>(path:string){return this.http.get<T>('/api/'+path);}post<T>(path:string,body:unknown){return this.http.post<T>('/api/'+path,body);}put<T>(path:string,body:unknown){return this.http.put<T>('/api/'+path,body);}delete(path:string){return this.http.delete('/api/'+path);}file(path:string){return this.http.get('/api/'+path,{responseType:'blob'});}saveFile(blob:Blob,name:string){const url=URL.createObjectURL(blob);const link=document.createElement('a');link.href=url;link.download=name;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}download(id:string){this.http.get('/api/ventas/'+id+'/comprobante',{responseType:'blob'}).subscribe({next:blob=>{const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download='comprobante.pdf';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);},error:()=>alert('No se pudo descargar el comprobante. Intenta nuevamente.')});}}
@Injectable({providedIn:'root'})
export class Cart {
 lines=signal<CartLine[]>([]);count=computed(()=>this.lines().reduce((sum,l)=>sum+l.quantity,0));amounts=computed(()=>totals(this.lines()));
 add(product:Product){const line=this.lines().find(l=>l.product.id===product.id);const qty=(line?.quantity??0)+1;if(!validQuantity(qty,product.stock))return;this.lines.update(lines=>line?lines.map(l=>l.product.id===product.id?{product,quantity:qty}:l):[...lines,{product,quantity:1}]);}
 quantity(id:string,quantity:number){this.lines.update(lines=>lines.map(l=>l.product.id===id&&validQuantity(quantity,l.product.stock)?{...l,quantity}:l));}
 remove(id:string){this.lines.update(lines=>lines.filter(l=>l.product.id!==id));}clear(){this.lines.set([]);}
}
export function errorMessage(error:unknown):string {if(error instanceof HttpErrorResponse)return (error.error?.errors?Object.values(error.error.errors).flat().join(' '):null)??error.error?.detail??error.error?.title??(error.status===0?'No pudimos conectar con Firmeza. Intenta nuevamente.':'Revisa los datos e intenta nuevamente.');return error instanceof Error?error.message:'No se pudo completar la operación.';}
