import { inject } from '@angular/core';
import { CanActivateFn, CanActivateChildFn, Router, Routes } from '@angular/router';
import { Session } from './core';
import { homeFor, roleDestination, validSession } from './access.mjs';
import { Catalog } from './catalog';
import { AuthPage } from './auth';
import { CartPage } from './cart';
import { Orders } from './orders';
import { Vehicles } from './vehicles';
const roleGuard = (role:string):CanActivateFn => () => {
 const destination=roleDestination(inject(Session).auth(),role);
 return destination?inject(Router).createUrlTree([destination]):true;
};
const administrator=roleGuard('Administrador');
const adminChild:CanActivateChildFn=()=>{
 const destination=roleDestination(inject(Session).auth(),'Administrador');
 return destination?inject(Router).createUrlTree([destination]):true;
};
const store:CanActivateFn=()=>inject(Session).auth()?.role==='Administrador'?inject(Router).createUrlTree(['/admin']):true;
const guest:CanActivateFn=()=>{const auth=inject(Session).auth();return validSession(auth)?inject(Router).createUrlTree([homeFor(auth!.role)]):true;};
export const routes:Routes=[
 {path:'admin',canActivate:[administrator],canActivateChild:[adminChild],loadComponent:()=>import('./admin/layout').then(m=>m.AdminLayout),children:[
  {path:'',pathMatch:'full',loadComponent:()=>import('./admin/dashboard').then(m=>m.AdminDashboard)},
  {path:'productos',data:{resource:'productos'},loadComponent:()=>import('./admin/inventory').then(m=>m.AdminInventory)},
  {path:'clientes',data:{resource:'clientes'},loadComponent:()=>import('./admin/inventory').then(m=>m.AdminInventory)},
  {path:'vehiculos',data:{resource:'vehiculos'},loadComponent:()=>import('./admin/inventory').then(m=>m.AdminInventory)},
  {path:'ventas',loadComponent:()=>import('./admin/sales').then(m=>m.AdminSales)},
  {path:'alquileres',loadComponent:()=>import('./admin/rentals').then(m=>m.AdminRentals)},
  {path:'archivos',loadComponent:()=>import('./admin/exchange').then(m=>m.AdminExchange)},
  {path:'**',redirectTo:''}
 ]},
 {path:'',component:Catalog,canActivate:[store]},
 {path:'ingresar',component:AuthPage,canActivate:[guest]},
 {path:'registro',component:AuthPage,canActivate:[guest],data:{register:true}},
 {path:'carrito',component:CartPage,canActivate:[store]},
 {path:'compras',component:Orders,canActivate:[roleGuard('Cliente')]},
 {path:'vehiculos',component:Vehicles,canActivate:[store]},
 {path:'**',redirectTo:''}
];
