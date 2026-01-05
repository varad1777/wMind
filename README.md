WMIND – Wonderbiz Manufacturing Intelligence & Networking Devices
📌 Overview
WMIND (Wonderbiz Manufacturing Intelligence and Networking Devices) is a full-stack, industrial manufacturing intelligence platform designed to collect, manage, visualize, and analyze real-time data from industrial devices.
The platform enables seamless communication between field-level devices and asset-level systems, supporting real-time monitoring, historical analysis, reporting, and AI-powered Root Cause Analysis (RCA).
 WMIND is built using a modular, event-driven, and scalable microservice architecture, making it suitable for modern smart manufacturing and Industry 4.0 environments.

🎯 Problem Statement

Manufacturing environments often suffer from:

Disconnected industrial devices
Limited real-time visibility into machine health
Manual and time-consuming troubleshooting of failures
Poor scalability and lack of advanced analytics


WMIND addresses these challenges by providing:

Centralized device and asset management
Real-time and historical signal monitoring
Automated alerts and notifications
Intelligent Root Cause Analysis (RCA) using Large Language Models

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
Enables reliable, asynchronous communication
Ensures loose coupling between services
Improves scalability and fault tolerance

🔹 Time-Series Database (InfluxDB)
Stores high-frequency signal data with timestamps
Optimized for time-based querying and analytics

🔹 RCA Service (Node.js + LLMs)
Performs intelligent Root Cause Analysis
Uses LLMs (LLaMA, Gemini) with Qdrant vector database
Provides contextual explanations for anomalies

🔹 Relational Database (SQL Server)
Stores metadata, configurations, users, and mappings

🛠️ Technology Stack

Frontend

ReactJS
Recharts
Driver.js (Guided User Tour)
Axios
Tailwind CSS, and shadcn/ui

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
Bulk upload using CSV / Excel
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

🧠 Root Cause Analysis (RCA

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

📈 Future Enhancements

Advanced visualization dashboards
Predictive analytics and forecasting
Configurable alarm thresholds and alert rules

Mobile application support
Full CI/CD automation

🏁 Conclusion

WMIND is a scalable, secure, and intelligent manufacturing intelligence platform that bridges the gap between industrial devices and actionable insights.
With real-time monitoring, advanced analytics, AI-powered Root Cause Analysis, and modern deployment practices, WMIND is well-positioned for smart factory and Industry 4.0 use cases.

🐳 Dockerized Project – Run Locally Guide

This section provides a step-by-step guide for panel members, reviewers, or developers to clone, configure, and run the complete WMIND microservices system locally using Docker Compose.
The instructions are intentionally simple and linear, so the project can be set up without prior context.

📋 Prerequisites

Ensure the following are installed on your system:

Docker
Docker Compose
Git

Step 1: Clone the Repository

Clone the project repository and navigate to the project root directory:

git clone <repository-url>
cd <project-root>

⚠️ All commands below must be executed from the project root directory.

Step 2: Create docker-compose.yml

In the project root directory, create a file named:

docker-compose.yml

Paste the Docker Compose configuration provided by the project team into this file.

The compose file defines and orchestrates the following services:

SQL Server
InfluxDB
RabbitMQ
Qdrant (Vector Database)
Backend microservices (Auth, Device, Asset)
API Gateway
AI / RCA Server
Frontend (React)

Step 3: Create .env File

In the same project root directory, create a file named:

.env

Paste the environment variables shared by the project team into this file.

Example environment variables:
SA_PASSWORD=
JWT_KEY=

RABBIT_USER=
RABBIT_PASS=

INFLUX_ORG=
INFLUX_BUCKET=

GROQ_API_KEY=
GEMINI_API_KEY=


⚠️ Do NOT start all services yet.
InfluxDB requires manual initialization before the full system can run.

Step 4: Build Docker Images

Build all application images using Docker Compose:

docker compose build


This step builds:

Backend microservices

API Gateway

AI / RCA Server

Frontend

Step 5: Start InfluxDB Only (Initial Setup)

InfluxDB must be started first to generate an authentication token.

Run only the InfluxDB container:

docker compose up -d influxdb


Wait until the container is healthy.

Step 6: Initialize InfluxDB & Generate Token

Open the InfluxDB UI in your browser:

http://localhost:8086


Complete the initial setup:

Create a username and password

Set Organization Name as Wonderbiz

Create a bucket

Generate an API Token with access to the bucket

📌 Copy and save the generated token.

Step 7: Update .env with Influx Token

Open the .env file and update the following variables:

INFLUX_TOKEN=<paste-generated-token>
INFLUX_ORG=<your-org-name>
INFLUX_BUCKET=<your-bucket-name>


⚠️ This step is mandatory
Without the InfluxDB token:

Asset Service will fail

AI / RCA Server will not start

Step 8: Start Remaining Services

Now start all remaining services:

docker compose up -d


Docker Compose will start:

SQL Server

RabbitMQ

Qdrant

Auth, Device, and Asset services

API Gateway

AI / RCA Server

Frontend

Step 9: Access the Application

Once all containers are running, access the services using the following URLs:

Frontend: http://localhost:3000

API Gateway: http://localhost:5000

RabbitMQ UI: http://localhost:15672

InfluxDB UI: http://localhost:8086

Step 10: Verify Running Containers

Check the status of running containers:

docker ps


View logs if required:

docker compose logs -f

Step 11: Stop the Application

To stop all services:

docker compose down


To stop services and remove volumes:

docker compose down -v

✅ Conclusion

Following this guide allows reviewers and developers to:

Correctly set up all required infrastructure

Safely initialize InfluxDB

Run the complete WMIND microservices system locally

Test the application without manual dependency installation

Prepared by: WMIND Project Team
