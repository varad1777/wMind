WMIND – Wonderbiz Manufacturing Intelligence & Networking Devices
📌 Overview

WMIND (Wonderbiz Manufacturing Intelligence and Networking Devices) is a full-stack industrial manufacturing intelligence platform designed to collect, manage, visualize, and analyze real-time data from industrial devices.

The platform enables seamless communication between field-level devices and asset-level systems, supporting:

Real-time monitoring

Historical analysis

Reporting

AI-powered Root Cause Analysis (RCA)

WMIND is built using a modular, event-driven, and scalable microservice architecture, making it suitable for Industry 4.0 and smart manufacturing environments.

🎯 Problem Statement

Manufacturing environments often suffer from:

Disconnected industrial devices

Limited real-time visibility into machine health

Manual and time-consuming troubleshooting

Poor scalability and lack of advanced analytics

WMIND addresses these challenges by providing:

Centralized device and asset management

Real-time and historical signal monitoring

Automated alerts and notifications

Intelligent RCA using Large Language Models

🧠 Key Capabilities

Industrial device onboarding and configuration

ModbusTCP-based data acquisition

Asset hierarchy management and signal mapping

Event-driven data flow using RabbitMQ

Time-series data storage using InfluxDB

Real-time and historical signal visualization

Report generation (CSV, PDF, Excel)

Role-based user access and security

AI-powered Root Cause Analysis (RCA)

Containerized deployment using Docker

🏗️ High-Level Architecture

WMIND follows a distributed microservice architecture to ensure scalability, reliability, and loose coupling.

🔹 Web Client (React)

Secure user interface

Device, asset, signal, report, and analytics management

🔹 Device Service (.NET Core)

Manages device onboarding and configuration

Communicates with industrial devices using ModbusTCP

Publishes real-time signal data to RabbitMQ

🔹 Asset Service (.NET Core)

Manages asset hierarchy and configurations

Consumes signal data from RabbitMQ

Maps signals to their corresponding assets

🔹 Message Broker (RabbitMQ)

Reliable asynchronous communication

Loose coupling between services

Improved scalability and fault tolerance

🔹 Time-Series Database (InfluxDB)

Stores high-frequency signal data with timestamps

Optimized for time-based querying and analytics

🔹 RCA Service (Node.js + LLMs)

Intelligent Root Cause Analysis

Uses LLaMA / Gemini with Qdrant vector database

Provides contextual explanations for anomalies

🔹 Relational Database (SQL Server)

Stores metadata, configurations, users, and mappings

🛠️ Technology Stack
Frontend

ReactJS

Recharts

Driver.js (Guided User Tour)

Axios

Tailwind CSS & shadcn/ui

Backend

ASP.NET Core (C#)

Node.js (RCA Service)

Databases

SQL Server (Metadata & Configurations)

InfluxDB (Time-Series Signal Data)

Qdrant (Vector Database)

Messaging & Protocols

RabbitMQ

ModbusTCP

DevOps & Deployment

Docker

Container-based hosting environment

Testing

Selenium (UI Automation)

PyTest

📊 Core Features
🔧 Device Management

Add, update, and delete devices

Bulk upload via CSV / Excel

Soft delete and recovery

Secure access with Two-Factor Authentication (2FA)

🏭 Asset Management

Hierarchical asset structure

Signal selection per asset

Device-to-asset mapping

📡 Signal Monitoring

Real-time signal visualization

Historical trend analysis

Zoom and spike inspection

🔔 Notifications

Automatic alert generation

Read / unread status tracking

Persistent notification history

📑 Reporting

Date-range based reports

CSV, PDF, and Excel export

Asset and signal-based filtering

🧠 Root Cause Analysis (RCA)

AI-driven anomaly explanation

LLM-powered insights

Faster troubleshooting and diagnosis

🔐 Security

JWT-based authentication

Two-Factor Authentication (2FA)

Role-based access control

Secure REST APIs

🐳 Deployment

Dockerized frontend and backend services

Environment-specific configurations

Ready for CI/CD integration

📈 Future Enhancements

Advanced visualization dashboards

Predictive analytics and forecasting

Configurable alarm thresholds and alert rules

Mobile application support

Full CI/CD automation

🐳 Run Locally – Dockerized Setup Guide
📋 Prerequisites

Ensure the following are installed:

Docker

Docker Compose

Git

Step 1: Clone the Repository
git clone <repository-url>
cd <project-root>

Step 2: Create docker-compose.yml

Create a file in the project root:

docker-compose.yml


Paste the provided Docker Compose configuration.
This includes:

SQL Server

InfluxDB

RabbitMQ

Qdrant

Backend services

API Gateway

AI / RCA Server

Frontend

Step 3: Create .env File
.env


Example variables:

SA_PASSWORD=
JWT_KEY=

RABBIT_USER=
RABBIT_PASS=

INFLUX_ORG=
INFLUX_BUCKET=

GROQ_API_KEY=
GEMINI_API_KEY=


⚠️ Do not start all services yet.

Step 4: Build Docker Images
docker compose build

Step 5: Start InfluxDB Only
docker compose up -d influxdb

Step 6: Initialize InfluxDB

Open:

http://localhost:8086


Create username & password

Set organization: Wonderbiz

Create bucket

Generate API token

Step 7: Update .env
INFLUX_TOKEN=<generated-token>
INFLUX_ORG=<org-name>
INFLUX_BUCKET=<bucket-name>

Step 8: Start All Services
docker compose up -d

Step 9: Access Services

Frontend: http://localhost:3000

API Gateway: http://localhost:5000

RabbitMQ UI: http://localhost:15672

InfluxDB UI: http://localhost:8086

Step 10: Verify Containers
docker ps
docker compose logs -f

Step 11: Stop Application
docker compose down
docker compose down -v

🏁 Conclusion

WMIND is a scalable, secure, and intelligent manufacturing intelligence platform that bridges the gap between industrial devices and actionable insights.

With real-time monitoring, advanced analytics, AI-powered Root Cause Analysis, and modern deployment practices, WMIND is well-positioned for smart factory and Industry 4.0 use cases.

Prepared by: WMIND Project Team
