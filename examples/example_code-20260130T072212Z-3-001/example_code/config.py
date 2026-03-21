#!/usr/bin/env python3
"""
Configuration file for Frappe API
"""

import os
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Frappe Configuration
FRAPPE_URL = os.getenv("FRAPPE_URL", "http://localhost:8000")
API_KEY = os.getenv("FRAPPE_API_KEY", "")
API_SECRET = os.getenv("FRAPPE_API_SECRET", "")

# Validation
if not API_KEY or not API_SECRET:
    raise ValueError("API_KEY and API_SECRET must be set in .env file")

# Optional: Use username/password instead
USERNAME = os.getenv("Administrator", "")
PASSWORD = os.getenv("User@home", "")

