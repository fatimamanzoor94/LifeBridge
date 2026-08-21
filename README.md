# 🩸 LifeBridge – Blood Donation Management System

LifeBridge is a full-stack **Blood Donation Management System** designed to digitally connect **Donors, Receivers, Hospitals, and Administrators** through a centralized platform.

The system streamlines blood request management, donor matching, hospital workflows, blood inventory management, donation tracking, notifications, and role-based operations.

LifeBridge was developed as a web-based application using **ASP.NET MVC, C#, Microsoft SQL Server, HTML5, CSS3, JavaScript, Bootstrap, Tailwind CSS, and Entity Framework**.

---

## 📌 Project Overview

Finding compatible blood donors during emergencies can be difficult when information is scattered across different sources.

LifeBridge provides a centralized system where:

- Receivers can submit blood requests.
- Hospitals can review and manage requests.
- Hospitals can check available blood inventory.
- The system can identify compatible donors.
- Hospitals can assign suitable donors.
- Donors can respond to assigned requests.
- Administrators can manage the overall platform.
- Users can receive system notifications and updates.

The goal of LifeBridge is to make blood donation management more organized, efficient, and accessible.

---

# ✨ Key Features

## 🔐 Authentication & Authorization

- User registration and login
- Role-based authentication
- Admin authentication
- Donor authentication
- Receiver authentication
- Hospital authentication
- Password reset functionality
- Secure password hashing using BCrypt
- Session-based authentication

---

## 👨‍💼 Admin Panel

The Admin Panel provides centralized control over the system.

### Admin capabilities include:

- User management
- Donor management
- Receiver management
- Hospital management
- Blood request management
- Hospital approval management
- Announcements
- Notifications
- Dashboard statistics
- System activity management
- Smart matching overview

---

## 🩸 Donor Panel

Donors can manage their donation-related activities.

### Donor features:

- Donor dashboard
- Donor profile management
- Blood group information
- Donation history
- Assigned blood requests
- SmartMatch requests
- Blood compatibility information
- Accept / Reject donor assignments
- Donation tracking
- Notifications

---

## 🏥 Hospital Panel

Hospitals have a dedicated management panel for handling blood requests and blood inventory.

### Hospital features:

- Hospital dashboard
- Hospital profile management
- Blood inventory management
- Blood collection history
- Blood issue history
- Low-stock monitoring
- Expiring blood monitoring
- Incoming blood requests
- Emergency blood requests
- Request tracking
- Smart Donor Search
- Donor assignment
- Assigned donor management
- Donor responses
- Notifications

---

## 👤 Receiver Panel

Receivers can submit and monitor their blood requirements.

### Receiver features:

- Receiver dashboard
- Blood request creation
- Multi-step blood request wizard
- Blood group selection
- Units required
- Hospital selection
- Blood compatibility preview
- Blood availability checking
- Compatible donor estimation
- Request tracking
- Emergency request support
- Notifications

---

# 🧠 Smart Donor Matching

LifeBridge includes a Smart Donor Matching workflow that helps hospitals find compatible donors when available blood inventory is insufficient.

### Workflow:

1. Receiver creates a blood request.
2. Hospital receives the request.
3. Hospital checks available blood inventory.
4. If sufficient blood is available, the hospital can issue blood from inventory.
5. If inventory is insufficient, Smart Donor Search can be used.
6. Compatible donors are identified.
7. Hospital selects and assigns a suitable donor.
8. Assigned donor receives the request.
9. Donor can accept or reject the assignment.
10. The donation workflow can then proceed.

---

# 🏦 Blood Inventory Management

Hospitals can manage their available blood stock through a centralized inventory system.

### Inventory features:

- Blood group-wise inventory
- Blood quantity tracking
- Blood collection records
- Blood issue records
- Low-stock monitoring
- Expiring blood monitoring
- Inventory availability checking

---

# 🔔 Notification System

LifeBridge includes a notification system to keep users informed about important activities.

Notifications can be used for:

- Blood request updates
- Hospital approval
- Donor assignments
- Donor responses
- Announcements
- System updates

The application also includes real-time communication infrastructure using **SignalR Hubs** where applicable.

---

# 🩸 Blood Compatibility

The system provides blood compatibility information to help users understand compatible blood groups during the blood request and donor matching process.

The compatibility functionality is integrated into the request and donor-matching workflows.

---

# 🚨 Emergency Blood Requests

LifeBridge supports emergency blood requests so urgent blood requirements can be prioritized and managed through the hospital and receiver workflows.

---

# 🛠️ Technologies Used

## Frontend

- **HTML5** – Page structure and semantic markup
- **CSS3** – Styling and responsive layouts
- **JavaScript (ES6)** – Client-side functionality and dynamic interactions
- **Bootstrap** – Responsive UI components and layouts
- **Tailwind CSS** – Utility-based styling for selected interfaces

## Backend

- **C#** – Core backend programming language
- **ASP.NET MVC** – Web application architecture and backend framework
- **Entity Framework** – Database access and ORM
- **BCrypt** – Secure password hashing
- **SignalR** – Real-time communication and notification infrastructure

## Database

- **Microsoft SQL Server** – Relational database
- **Entity Framework** – Database interaction and data management

## APIs & Integrations

- **Gmail SMTP** – Email notification functionality
- **Google Maps API** – Location and distance-related functionality
- **WhatsApp API** – WhatsApp communication/notification functionality

## Development Tools

- **Visual Studio** – Application development
- **Git** – Version control
- **GitHub** – Source code management and repository hosting

---

# 🏗️ Architecture

LifeBridge follows the **ASP.NET MVC architecture**.

```text
User
  ↓
View / UI
  ↓
Controller
  ↓
Service / Business Logic
  ↓
Entity Framework
  ↓
SQL Server Database
