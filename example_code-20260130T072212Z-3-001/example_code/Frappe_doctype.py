from frappeclient import FrappeClient

client = FrappeClient("http://localhost:8000")
client.authenticate("f2c7078ccc3bcc2", "7d6c8765f2f964e")

doctypes = client.get_list("DocType", 
                          fields=["name"],
                          limit_page_length=999999)  # Large number

print(f"Total DocTypes: {len(doctypes)}\n")
for dt in doctypes:
    print(dt['name'])



meta = client.get_doc("DocType", "Item")

for field in meta.get("fields", []):
    if field['fieldtype'] == 'Link':
        print(f"{field['fieldname']:30} → {field.get('options')}")



"""
Common useful DocTypes:

    Item
    Customer
    Supplier
    Sales Order
    Purchase Order
    Sales Invoice
    Purchase Invoice
    Stock Entry
    Quotation
    Delivery Note
"""
# test for checking the need to child table.. 

doctype_meta = client.get_doc("DocType", "Purchase Invoice")

# Check for child tables
for field in doctype_meta['fields']:
    if field['fieldtype'] == 'Table':
        print(f"Child table: {field['fieldname']} -> {field['options']}")



