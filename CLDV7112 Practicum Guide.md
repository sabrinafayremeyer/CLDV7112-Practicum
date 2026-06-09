# CLDV7112 Practicum Guide

**Total Marks: 100**

## Overview

Build a real-time processing system for a GameStop online inventory management platform. The system must track and manage games stored and sold online, using a scalable cloud-native solution capable of handling heavy traffic spikes during major sales events while ensuring low latency and high availability.

**Stack:** Azure-hosted MVC application with a real-time data processing pipeline.

---

## Part A: Building a Real-Time Processing System

### a. Web Hosting

Create an **Azure App Service** to host the main MVC web application.

---

### b. Relational Database (User Management)

Set up an **Azure SQL Database** to manage user accounts and roles.

- You are **not** required to implement full ASP.NET Core Identity
- Create a simple relational table structure storing:
  - Customer login details (e.g. username, password, email)
  - Assigned roles (e.g. Admin, Customer)
- Provide proof that:
  - The database is hosted in Azure
  - It is correctly connected to the web app

---

### c. Cloud Storage (Data & Media)

Utilise both unstructured and NoSQL storage solutions:

| Service | Purpose |
|---|---|
| **Azure Blob Storage** | Store all game cover images uploaded through the web application |
| **Azure Table Storage** | Store non-relational data such as system audit logs or rapid-load inventory categorisations |

---

### d. Serverless Data Pipeline

Build a scalable data processing pipeline using exactly **4 Azure Functions**:

| # | Trigger Type | Description |
|---|---|---|
| 1 | **Blob Triggered** | Fires when a new game image is uploaded to Blob Storage — processes it (e.g. creates a thumbnail) and writes an audit log entry to Azure Table Storage |
| 2 | **HTTP Triggered (GET)** | Called by the web app when a user views a specific game — fetches real-time stock levels or metadata |
| 3 | **HTTP Triggered (POST)** | Called when a user clicks "Buy" or "Add to Cart" — places an order message into an Azure Storage Queue |
| 4 | **Queue Triggered** | Listens to the Storage Queue, processes incoming order messages, and updates the final inventory count in Azure SQL Database or Azure Table Storage |

> The above are examples. You may change what each function does, but the trigger types (Blob, GET, POST, Queue) must be preserved.

---

### e. Load Testing

Simulate heavy traffic to test system scalability. Use one of:

- Azure Load Testing
- Apache JMeter
- A custom automated script

Simulate multiple concurrent users hitting your App Service.

---

### f. Monitoring

Monitor performance and resource utilisation during simulated peak load using:

- Azure Monitor, and/or
- Application Insights

Capture system response metrics during the load test.

---

### g. Auto Scaling

Configure **scale-out auto-scaling rules** for your Azure App Service based on CPU or memory thresholds.

Provide screenshots showing the application successfully scaling to handle the traffic simulated in step e.

---

## Part B: Theory Questions

### Question 1

Document how scaling Azure PaaS/Serverless applications (Azure App Service, Azure Functions, Azure SQL DB) differs from scaling applications in a traditional IaaS (Infrastructure as a Service) model.

### Question 2

Discuss the concepts of **platform-level** vs **infrastructure-level** scaling, focusing on:

- Managed services vs self-managed infrastructure
- The impact these choices have on:
  - Deployment time
  - Management overhead
  - Cost

### Question 3

Use a **comparison table** to contrast Azure PaaS/Serverless scaling with traditional IaaS scaling to illustrate the concepts discussed above.
