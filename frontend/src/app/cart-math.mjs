export const roundMoney = value => Math.round((value + Number.EPSILON) * 100) / 100;
export function totals(lines) {
 const subtotal = roundMoney(lines.reduce((sum, line) => sum + line.product.price * line.quantity, 0));
 const tax = roundMoney(subtotal * 0.19);
 return { subtotal, tax, total: roundMoney(subtotal + tax) };
}
export function validQuantity(value, stock) { return Number.isInteger(value) && value > 0 && value <= stock; }
