#!/usr/bin/env python3
"""
Advanced Frappe API operations
"""
from frappeclient import FrappeClient
import json
from datetime import datetime, timedelta

# Configuration
# Configuration
BASE_URL = "http://localhost:8000"
API_KEY = "f2c7078ccc3bcc2"
API_SECRET = "7d6c8765f2f964e"


client = FrappeClient(BASE_URL)
client.authenticate(API_KEY, API_SECRET)

#client = FrappeClient(BASE_URL, API_KEY, API_SECRET)

def create_item():
    """Create a product item"""
    print("\n=== Creating Item ===")
    item = client.insert({
        "doctype": "Item",
        "item_code": "LAPTOP-001",
        "item_name": "Dell Laptop",
        "item_group": "Products",
        "stock_uom": "Nos",
        "is_stock_item": 1,
        "standard_rate": 50000
    })
    print(f"Created Item: {item['name']}")
    return item['name']

def create_sales_order(customer_id):
    """Create a sales order with items"""
    print("\n=== Creating Sales Order ===")
    
    delivery_date = (datetime.now() + timedelta(days=7)).strftime("%Y-%m-%d")
    
    sales_order = client.insert({
        "doctype": "Sales Order",
        "customer": customer_id,
        "delivery_date": delivery_date,
        "items": [
            {
                "item_code": "LAPTOP-001",
                "qty": 2,
                "rate": 50000
            },
            {
                "item_code": "Mouse",
                "qty": 2,
                "rate": 500
            }
        ]
    })
    print(f"Created Sales Order: {sales_order['name']}")
    print(f"Total Amount: {sales_order.get('grand_total', 'N/A')}")
    return sales_order['name']

def get_value(doctype, name, fieldname):
    """Get specific field value"""
    print(f"\n=== Getting {fieldname} from {doctype} ===")
    value = client.get_value(doctype, name, fieldname)
    print(f"{fieldname}: {value}")
    return value

def set_value(doctype, name, fieldname, value):
    """Set specific field value"""
    print(f"\n=== Setting {fieldname} in {doctype} ===")
    result = client.set_value(doctype, name, fieldname, value)
    print(f"Updated {fieldname} to: {value}")
    return result

def search_customers(query):
    """Search customers by text"""
    print(f"\n=== Searching for '{query}' ===")
    # Using get_list with filters
    customers = client.get_list(
        "Customer",
        fields=["name", "customer_name", "customer_type"],
        filters=[["customer_name", "like", f"%{query}%"]],
        limit_page_length=20
    )
    print(f"Found {len(customers)} results:")
    for customer in customers:
        print(f"  - {customer['name']}: {customer['customer_name']}")
    return customers

def get_count(doctype, filters=None):
    """Get document count"""
    print(f"\n=== Counting {doctype} ===")
    count = client.get_count(doctype, filters)
    print(f"Total: {count}")
    return count

def call_custom_method():
    """Call a custom whitelisted method"""
    print("\n=== Calling Custom Method ===")
    try:
        # Example: Get logged user
        result = client.get_api("frappe.auth.get_logged_user")
        print(f"Logged user: {result}")
        return result
    except Exception as e:
        print(f"Error calling method: {e}")

def submit_document(doctype, name):
    """Submit a submittable document"""
    print(f"\n=== Submitting {doctype} {name} ===")
    doc = client.get_doc(doctype, name)
    if doc.get('docstatus') == 0:  # Draft
        client.submit(doctype, name)
        print(f"Submitted: {name}")
    else:
        print(f"Already submitted (status: {doc.get('docstatus')})")

def cancel_document(doctype, name):
    """Cancel a submitted document"""
    print(f"\n=== Cancelling {doctype} {name} ===")
    doc = client.get_doc(doctype, name)
    if doc.get('docstatus') == 1:  # Submitted
        client.cancel(doctype, name)
        print(f"Cancelled: {name}")
    else:
        print(f"Cannot cancel (status: {doc.get('docstatus')})")

def main():
    try:
        # Create customer first
        customer_id = client.insert({
            "doctype": "Customer",
            "customer_name": "Tech Corp",
            "customer_type": "Company",
            "customer_group": "Commercial",
            "territory": "All Territories"
        })['name']
        print(f"Created customer: {customer_id}")
        
        # Get specific field
        customer_name = get_value("Customer", customer_id, "customer_name")
        
        # Update specific field
        set_value("Customer", customer_id, "mobile_no", "+1234567890")
        
        # Search
        search_customers("Tech")
        
        # Count documents
        get_count("Customer", [["customer_type", "=", "Company"]])
        
        # Call custom method
        call_custom_method()
        
        # Create item and sales order
        # item_id = create_item()
        # so_id = create_sales_order(customer_id)
        
        # Submit document
        # submit_document("Sales Order", so_id)
        
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()
