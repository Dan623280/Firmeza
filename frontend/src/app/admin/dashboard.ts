import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Api, errorMessage } from '../core';
import { Dashboard } from '../models';
@Component({standalone:true,imports:[CurrencyPipe,RouterLink],template:`
<span class="eyebrow">VISTA GENERAL</span><h1>Tu negocio, en perspectiva<span>.</span></h1><p>Consulta la actividad y prepara tu próxima operación.</p>
@if(error()){<p class="notice error" role="alert">{{error()}} <button class="text-button" (click)="load()">Reintentar</button></p>}
@if(loading()){<p role="status">Cargando métricas…</p>}
@if(stats();as s){<div class="admin-stats"><a routerLink="/admin/productos" class="card"><small>Productos</small><strong>{{s.products}}</strong><span>Ver inventario →</span></a><a routerLink="/admin/clientes" class="card"><small>Clientes</small><strong>{{s.customers}}</strong><span>Ver clientes →</span></a><a routerLink="/admin/ventas" class="card"><small>Ventas</small><strong>{{s.sales}}</strong><span>Consultar ventas →</span></a><a routerLink="/admin/alquileres" class="card"><small>Alquileres</small><strong>{{s.rentals}}</strong><span>Ver reservas →</span></a></div><div class="card admin-revenue"><small>Ingresos de ventas confirmadas · IVA incluido</small><strong>{{s.revenue|currency:'COP':'symbol-narrow':'1.0-0'}}</strong></div>}
<div class="admin-banner"><div><span class="eyebrow">CONSTRUIMOS CONTIGO</span><h2>Todo listo para tu próxima venta.</h2><p>Mantén tu inventario al día y acompaña cada proyecto.</p></div><a routerLink="/admin/ventas" class="button">Registrar una venta →</a></div>`})
export class AdminDashboard {
 api=inject(Api);stats=signal<Dashboard|null>(null);loading=signal(false);error=signal('');
 constructor(){this.load();}
 load(){this.loading.set(true);this.error.set('');this.api.get<Dashboard>('admin/dashboard').subscribe({next:s=>{this.stats.set(s);this.loading.set(false);},error:e=>{this.error.set(errorMessage(e));this.loading.set(false);}});}
}
