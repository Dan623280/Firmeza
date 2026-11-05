import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Api, errorMessage } from '../core';
type Row=Record<string,string|number|boolean> & {id:string;active:boolean};
interface Field {key:string;label:string;type:string;required?:boolean;min?:number;max?:number;step?:string;maxLength?:number;}
interface Config {title:string;singular:string;fields:Field[];}
const configs:Record<string,Config>={
 productos:{title:'Productos',singular:'producto',fields:[{key:'name',label:'Nombre',type:'text',required:true,maxLength:150},{key:'description',label:'Descripción',type:'textarea',maxLength:500},{key:'price',label:'Precio COP',type:'number',required:true,min:0.01,step:'0.01'},{key:'stock',label:'Stock',type:'number',required:true,min:0,step:'1'}]},
 clientes:{title:'Clientes',singular:'cliente',fields:[{key:'document',label:'Documento',type:'text',required:true,maxLength:30},{key:'name',label:'Nombre',type:'text',required:true,maxLength:100},{key:'lastName',label:'Apellido',type:'text',required:true,maxLength:100},{key:'email',label:'Correo',type:'email',required:true,maxLength:254},{key:'phone',label:'Teléfono',type:'tel',required:true,maxLength:30},{key:'address',label:'Dirección',type:'text',required:true,maxLength:250},{key:'age',label:'Edad',type:'number',required:true,min:18,max:120,step:'1'}]},
 vehiculos:{title:'Vehículos',singular:'vehículo',fields:[{key:'name',label:'Nombre',type:'text',required:true,maxLength:150},{key:'type',label:'Tipo',type:'text',required:true,maxLength:80},{key:'brand',label:'Marca',type:'text',required:true,maxLength:80},{key:'plate',label:'Placa',type:'text',required:true,maxLength:12},{key:'pricePerDay',label:'Precio por día COP',type:'number',required:true,min:0.01,step:'0.01'},{key:'available',label:'Disponible para reservar',type:'checkbox'}]}
};
@Component({standalone:true,imports:[FormsModule],template:`
<span class="eyebrow">GESTIÓN DEL NEGOCIO</span><h1>{{config.title}}<span>.</span></h1><p>Crea, actualiza y desactiva registros conservando su historial.</p>
@if(error()){<p class="notice error" role="alert">{{error()}}</p>}@if(message()){<p class="notice success" role="status">{{message()}}</p>}
<section class="card admin-editor"><div class="admin-section-heading"><h2>{{editingId?'Editar '+config.singular:'Nuevo '+config.singular}}</h2>@if(editingId){<button class="text-button" (click)="reset()">Cancelar edición</button>}</div>
<form #form="ngForm" (ngSubmit)="save()"><div class="admin-fields">
@for(field of config.fields;track field.key){<label [class.admin-check]="field.type==='checkbox'">{{field.label}}
@if(field.type==='textarea'){<textarea [name]="field.key" [(ngModel)]="model[field.key]" [maxlength]="field.maxLength??500" rows="3"></textarea>}
@else if(field.type==='number'){<input type="number" [name]="field.key" [(ngModel)]="model[field.key]" [required]="field.required??false" [min]="field.min??0" [max]="field.max??9999999999" [step]="field.step??'1'">}
@else if(field.type==='checkbox'){<input type="checkbox" [name]="field.key" [(ngModel)]="model[field.key]">}
@else{<input [type]="field.type" [name]="field.key" [(ngModel)]="model[field.key]" [required]="field.required??false" [attr.min]="field.min??null" [attr.max]="field.max??null" [attr.step]="field.step??null" [attr.maxlength]="field.maxLength??null" [email]="field.type==='email'">}
</label>}
<label class="admin-check"><input type="checkbox" name="active" [(ngModel)]="model['active']"> Activo</label></div><button class="button" [disabled]="form.invalid||saving()">{{saving()?'Guardando…':'Guardar '+config.singular}}</button></form></section>
<div class="admin-toolbar"><label class="admin-search">Buscar por nombre, documento o placa<input [ngModel]="search()" (ngModelChange)="search.set($event)" placeholder="Buscar…"></label><label>Estado<select [ngModel]="status()" (ngModelChange)="status.set($event)"><option value="all">Todos</option><option value="active">Activos</option><option value="inactive">Inactivos</option></select></label><button class="button secondary" [disabled]="loading()" (click)="load()">Actualizar</button></div>
@if(loading()){<p role="status">Cargando registros…</p>}
<div class="card admin-table-wrap"><table class="admin-table"><thead><tr>@for(field of config.fields;track field.key){<th>{{field.label}}</th>}<th>Estado</th><th>Acciones</th></tr></thead><tbody>
@for(row of filtered();track row.id){<tr>@for(field of config.fields;track field.key){<td>{{field.type==='checkbox'?(row[field.key]?'Sí':'No'):row[field.key]}}</td>}<td><span class="status">{{row.active?'Activo':'Inactivo'}}</span></td><td class="admin-row-actions"><button class="text-button" [disabled]="saving()" (click)="edit(row)">Editar</button>@if(row.active){<button class="admin-danger" [disabled]="saving()" (click)="deactivate(row)">Desactivar</button>}</td></tr>}
</tbody></table>@if(!loading()&&!filtered().length){<p class="admin-empty">No hay registros para mostrar.</p>}</div>`})
export class AdminInventory {
 api=inject(Api);route=inject(ActivatedRoute);resource='productos';config=configs['productos'];rows=signal<Row[]>([]);search=signal('');status=signal('all');loading=signal(false);saving=signal(false);error=signal('');message=signal('');editingId:string|null=null;model:Record<string,string|number|boolean>={};
 filtered=computed(()=>{const q=this.search().trim().toLocaleLowerCase();return this.rows().filter(r=>(this.status()==='all'||r.active===(this.status()==='active'))&&[r['name'],r['lastName'],r['document'],r['plate'],r['email']].join(' ').toLocaleLowerCase().includes(q));});
 constructor(){this.route.data.subscribe(data=>{this.resource=data['resource'];this.config=configs[this.resource];this.search.set('');this.status.set('all');this.reset();this.load();});}
 reset(){this.editingId=null;this.model={active:true};for(const field of this.config.fields)this.model[field.key]=field.type==='checkbox'?true:field.type==='number'?(field.key==='age'?18:0):'';this.error.set('');}
 load(){this.loading.set(true);this.error.set('');this.api.get<Row[]>(this.resource).subscribe({next:rows=>{this.rows.set(rows);this.loading.set(false);},error:e=>{this.error.set(errorMessage(e));this.loading.set(false);}});}
 edit(row:Row){this.model={...row};this.editingId=row.id;this.error.set('');this.message.set('');}
 save(){if(this.saving())return;for(const key of ['stock','age'])if(key in this.model&&!Number.isInteger(this.model[key])){this.error.set('La edad y el stock deben ser números enteros.');return;}for(const field of this.config.fields){const value=this.model[field.key];if(field.type==='number'&&((field.min!==undefined&&Number(value)<field.min)||(field.max!==undefined&&Number(value)>field.max))){this.error.set('Revisa el valor de '+field.label+'.');return;}}
 this.saving.set(true);this.error.set('');this.message.set('');const body={...this.model};delete body['id'];const request=this.editingId?this.api.put(this.resource+'/'+this.editingId,body):this.api.post(this.resource,body);
 request.subscribe({next:()=>{this.saving.set(false);this.reset();this.message.set('Registro guardado correctamente.');this.load();},error:e=>{this.saving.set(false);this.error.set(errorMessage(e));}});}
 deactivate(row:Row){if(!confirm('¿Desactivar este '+this.config.singular+'? Se conservará el historial.'))return;this.saving.set(true);this.error.set('');this.api.delete(this.resource+'/'+row.id).subscribe({next:()=>{this.saving.set(false);if(this.editingId===row.id)this.reset();this.message.set('Registro desactivado.');this.load();},error:e=>{this.saving.set(false);this.error.set(errorMessage(e));}});}
}
