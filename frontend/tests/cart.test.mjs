import test from 'node:test';import assert from 'node:assert/strict';import {totals,validQuantity} from '../src/app/cart-math.mjs';
test('calcula subtotal, IVA del 19% y total en COP',()=>assert.deepEqual(totals([{product:{price:34500},quantity:2}]),{subtotal:69000,tax:13110,total:82110}));
test('redondea el IVA a dos decimales',()=>assert.deepEqual(totals([{product:{price:0.03},quantity:1}]),{subtotal:0.03,tax:0.01,total:0.04}));
test('rechaza fracciones, negativos y cantidades superiores al stock',()=>{assert.equal(validQuantity(1.5,5),false);assert.equal(validQuantity(-1,5),false);assert.equal(validQuantity(6,5),false);assert.equal(validQuantity(5,5),true);});
