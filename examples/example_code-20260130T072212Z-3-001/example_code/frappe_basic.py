#!/usr/bin/env python3
"""
Basic Frappe API operations using frappe_client
"""

from frappeclient import FrappeClient
#from frappe_client import FrappeClient

# Configuration
BASE_URL = "http://localhost:8090"
API_KEY = "3618011e792669e"
API_SECRET = "b6cacddf7c94e5a"


client = FrappeClient(BASE_URL)
client.authenticate(API_KEY, API_SECRET)
# Initialize client
#client = FrappeClient(BASE_URL, API_KEY, API_SECRET)

def create_customer():
    """Create a new customer"""
    print("\n=== Creating Customer ===")
    customer = client.insert({
        "doctype": "Customer",
        "customer_name": "John Doe",
        "customer_type": "Individual",
        "customer_group": "Individual",
        "territory": "All Territories",
        "mobile_no": "+1234567890"
    })
    print(f"Created: {customer['name']}")
    return customer['name']

def get_customer(customer_id):
    """Get customer details"""
    print(f"\n=== Getting Customer {customer_id} ===")
    customer = client.get_doc("Customer", customer_id)
    print(f"Name: {customer['customer_name']}")
    print(f"Type: {customer['customer_type']}")
    print(f"Mobile: {customer.get('mobile_no', 'N/A')}")
    return customer

def update_customer(customer_id):
    """Update customer"""
    print(f"\n=== Updating Customer {customer_id} ===")
    customer = client.get_doc("Customer", customer_id)
    customer['mobile_no'] = "+9876543210"
    customer['email_id'] = "john.doe@example.com"
    updated = client.update(customer)
    print(f"Updated: {updated['name']}")
    return updated

def list_customers():
    """List all customers"""
    print("\n=== Listing Customers ===")
    customers = client.get_list(
        "Customer",
        fields=["name", "customer_name", "customer_type"],
        filters=[["customer_type", "=", "Individual"]],
        limit_page_length=10
    )
    print(f"Found {len(customers)} customers:")
    for customer in customers:
        print(f"  - {customer['name']}: {customer['customer_name']}")
    return customers

def delete_customer(customer_id):
    """Delete customer"""
    print(f"\n=== Deleting Customer {customer_id} ===")
    client.delete("Customer", customer_id)
    print(f"Deleted: {customer_id}")

def main():
    try:
        # Create
        customer_id = create_customer()
        
        # Read
        get_customer(customer_id)
        
        # Update
        update_customer(customer_id)
        
        # List
        list_customers()
        
        # Delete (commented out for safety)
        # delete_customer(customer_id)
        
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    main()
