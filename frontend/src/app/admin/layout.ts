import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Session } from '../core';
@Component({standalone:true,imports:[RouterLink,RouterLinkActive,RouterOutlet],template:`
<div class="admin-layout"><aside class="admin-sidebar"><span class="eyebrow">GESTIÓN DEL NEGOCIO</span><h2>Administración</h2><nav aria-label="Administración">
@for(link of links;track link.path){<a [routerLink]="link.path" routerLinkActive="selected" [routerLinkActiveOptions]="{exact:link.path==='/admin'}"><span aria-hidden="true">{{link.icon}}</span>{{link.label}}</a>}
</nav><div class="admin-identity"><strong>{{session.auth()?.name}}</strong><small>Rol: Administrador</small></div></aside><section class="admin-content"><router-outlet/></section></div>`})
export class AdminLayout {
 session=inject(Session);
 links=[{path:'/admin',label:'Inicio',icon:'◫'},{path:'/admin/productos',label:'Productos',icon:'▦'},{path:'/admin/clientes',label:'Clientes',icon:'◉'},{path:'/admin/ventas',label:'Ventas',icon:'▤'},{path:'/admin/vehiculos',label:'Vehículos',icon:'▱'},{path:'/admin/alquileres',label:'Alquileres',icon:'▣'},{path:'/admin/archivos',label:'Importar y exportar',icon:'⇅'}];
}
