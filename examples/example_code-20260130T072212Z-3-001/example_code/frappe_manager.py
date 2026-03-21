#!/usr/bin/env python3
"""
Frappe Document Manager - Complete CRUD operations
"""

#from frappe_client import FrappeClient
from frappeclient import FrappeClient
from typing import List, Dict, Any, Optional
import logging

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class FrappeManager:
    """Manager class for Frappe operations"""
    
    def __init__(self, url: str, api_key: str, api_secret: str):
        """Initialize Frappe client"""
        self.client = FrappeClient(url, api_key, api_secret)
        logger.info(f"Connected to Frappe at {url}")
    
    def create(self, doctype: str, data: Dict[str, Any]) -> Dict[str, Any]:
        """Create a new document"""
        try:
            data['doctype'] = doctype
            doc = self.client.insert(data)
            logger.info(f"Created {doctype}: {doc.get('name')}")
            return doc
        except Exception as e:
            logger.error(f"Error creating {doctype}: {e}")
            raise
    
    def read(self, doctype: str, name: str) -> Dict[str, Any]:
        """Read a document"""
        try:
            doc = self.client.get_doc(doctype, name)
            logger.info(f"Retrieved {doctype}: {name}")
            return doc
        except Exception as e:
            logger.error(f"Error reading {doctype} {name}: {e}")
            raise
    
    def update(self, doctype: str, name: str, data: Dict[str, Any]) -> Dict[str, Any]:
        """Update a document"""
        try:
            doc = self.client.get_doc(doctype, name)
            doc.update(data)
            updated = self.client.update(doc)
            logger.info(f"Updated {doctype}: {name}")
            return updated
        except Exception as e:
            logger.error(f"Error updating {doctype} {name}: {e}")
            raise
    
    def delete(self, doctype: str, name: str) -> bool:
        """Delete a document"""
        try:
            self.client.delete(doctype, name)
            logger.info(f"Deleted {doctype}: {name}")
            return True
        except Exception as e:
            logger.error(f"Error deleting {doctype} {name}: {e}")
            raise
    
    def list_all(self, doctype: str, fields: Optional[List[str]] = None, 
                 filters: Optional[List] = None, limit: int = 20) -> List[Dict]:
        """List documents with filters"""
        try:
            docs = self.client.get_list(
                doctype,
                fields=fields or ["name"],
                filters=filters or [],
                limit_page_length=limit
            )
            logger.info(f"Listed {len(docs)} {doctype} documents")
            return docs
        except Exception as e:
            logger.error(f"Error listing {doctype}: {e}")
            raise
    
    def search(self, doctype: str, query: str, field: str = "name") -> List[Dict]:
        """Search documents"""
        try:
            filters = [[field, "like", f"%{query}%"]]
            return self.list_all(doctype, filters=filters)
        except Exception as e:
            logger.error(f"Error searching {doctype}: {e}")
            raise
    
    def bulk_create(self, doctype: str, data_list: List[Dict]) -> List[Dict]:
        """Create multiple documents"""
        results = []
        for data in data_list:
            try:
                doc = self.create(doctype, data)
                results.append(doc)
            except Exception as e:
                logger.error(f"Error in bulk create: {e}")
                results.append({"error": str(e)})
        return results
    
    def get_count(self, doctype: str, filters: Optional[List] = None) -> int:
        """Get document count"""
        try:
            count = self.client.get_count(doctype, filters)
            logger.info(f"Count of {doctype}: {count}")
            return count
        except Exception as e:
            logger.error(f"Error counting {doctype}: {e}")
            raise


# Example usage
def main():
    # Configuration
    #URL = "http://site2.local:8000"
    #API_KEY = "your_api_key"
    #API_SECRET = "your_api_secret"
    
    BASE_URL = "http://localhost:8000"
    API_KEY = "f2c7078ccc3bcc2"
    API_SECRET = "7d6c8765f2f964e"
 
    manager = FrappeManager(BASE_URL, API_KEY, API_SECRET)
    
    # Initialize manager
    #manager = FrappeManager(URL, API_KEY, API_SECRET)
    
    # CREATE
    customer = manager.create("Customer", {
        "customer_name": "ACME Corporation",
        "customer_type": "Company",
        "customer_group": "Commercial",
        "territory": "All Territories"
    })
    customer_id = customer['name']
    
    # READ
    customer_data = manager.read("Customer", customer_id)
    print(f"Customer: {customer_data['customer_name']}")
    
    # UPDATE
    manager.update("Customer", customer_id, {
        "mobile_no": "+1234567890",
        "email_id": "contact@acme.com"
    })
    
    # LIST
    customers = manager.list_all(
        "Customer",
        fields=["name", "customer_name", "customer_type"],
        filters=[["customer_type", "=", "Company"]],
        limit=10
    )
    print(f"Found {len(customers)} companies")
    
    # SEARCH
    results = manager.search("Customer", "ACME", "customer_name")
    print(f"Search results: {len(results)}")
    
    # COUNT
    total = manager.get_count("Customer")
    print(f"Total customers: {total}")
    
    # BULK CREATE
    bulk_data = [
        {"customer_name": "Company A", "customer_type": "Company", 
         "customer_group": "Commercial", "territory": "All Territories"},
        {"customer_name": "Company B", "customer_type": "Company", 
         "customer_group": "Commercial", "territory": "All Territories"},
    ]
    # results = manager.bulk_create("Customer", bulk_data)
    
    # DELETE (commented for safety)
    # manager.delete("Customer", customer_id)

if __name__ == "__main__":
    main()
