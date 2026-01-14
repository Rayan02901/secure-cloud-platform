#!/bin/bash
# Wait for Grafana to start
sleep 30

# Import ASP.NET Core dashboard
curl -X POST http://admin:admin@localhost:3000/api/dashboards/db \
  -H "Content-Type: application/json" \
  -d '{
    "dashboard": {
      "id": null,
      "title": "ASP.NET Core Metrics",
      "tags": ["templated"],
      "timezone": "browser",
      "panels": [],
      "schemaVersion": 16,
      "version": 0
    },
    "overwrite": false
  }'

# Or use pre-built dashboard IDs:
# - 10464: ASP.NET Core Performance Counters
# - 10826: HTTP Metrics