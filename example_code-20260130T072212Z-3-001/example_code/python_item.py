from frappeclient import FrappeClient

client = FrappeClient("http://localhost:8000")
client.authenticate("f2c7078ccc3bcc2", "7d6c8765f2f964e")

"""
# Get Item DocType structure
doctype_meta = client.get_doc("DocType", "Item")

# Print all fields
for field in doctype_meta.get("fields", []):
    print(f"{field['fieldname']:30} | {field['fieldtype']:15} | Required: {field.get('reqd', 0)}")

"""
# List all DocTypes
doctypes = client.get_list("DocType", 
                          fields=["name", "module", "istable"],
                          limit_page_length=0)  # 0 = get all

# Print DocType names
for dt in doctypes:
    print(dt['name'])