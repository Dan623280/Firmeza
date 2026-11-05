export interface Product { id:string;name:string;description:string;price:number;stock:number;active:boolean; }
export interface Auth {token:string;expiresAt:string;name:string;role:string;}
export interface Sale {id:string;customerId:string;customerName:string;number:string;date:string;subtotal:number;tax:number;total:number;status:string;emailSent:boolean;details:{productName:string;quantity:number;subtotal:number}[];}
export interface Vehicle {id:string;name:string;type:string;brand:string;plate:string;pricePerDay:number;available:boolean;active:boolean;}
export interface Rental {id:string;vehicleName:string;startDate:string;endDate:string;pricePerDay:number;total:number;status:string;}
export interface CartLine {product:Product;quantity:number;}

export interface Customer {id:string;document:string;name:string;lastName:string;email:string;phone:string;address:string;age:number;active:boolean;}
export interface Dashboard {products:number;customers:number;sales:number;revenue:number;rentals:number;}
export interface ImportResult {products:number;customers:number;sales:number;errors:string[];}
