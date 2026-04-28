# XFrame: XML Transformation Suite

XFrame is a .NET MAUI desktop utility designed for developers and data analysts. It provides a robust environment for transforming XML via XSLT, executing custom  aggregations (Sum Engine), and managing data exports with native OS integration.

## 🚀 Core Features

1. **XML/XSLT Transformation**

Real-time Processing: Transform raw XML using XSLT stylesheets.

Thread Safety: All transformations are offloaded to background threads using Task.Run to ensure the UI remains responsive even during heavy processing.


2. **Custom Sum Engine**

Tag-Based Aggregation: Extract and sum numeric values based on specific XML tags.

Dual-Source Logic: Run calculations against either the Raw Source or the Transformed Result.

Precision Handling: Automatic normalization of decimal separators (comma/dot) for consistent math operations.

3. **Export System**

Native FileSaver: Integrated with CommunityToolkit.Maui to provide a native "Save As" experience.

User-Defined Destitinations: Select any directory and filename via native OS dialogs.

UTF-8 Integrity: Exports are handled via memory streams to ensure character encoding is maintained.

4. **Error & Notification Handling**

Service-Oriented Design: Centralized INotificationService.

Anti-Alert Storm: Uses SemaphoreSlim locking to prevent multiple overlapping modal dialogs.

Non-Intrusive Feedback: Success notifications are delivered via native Toasts/Snackbars to maintain workflow focus.


## 🛠 Project Structure

- XFrame: The primary .NET MAUI project containing ViewModels and XAML Views.

- XFrame.Core: Contains the business logic and definitions that are completely independent of any UI framework.

- XFrame.AppHost: The .NET Aspire orchestration project that manages the app lifecycle and provides the developer dashboard.

- XFrame.ServiceDefaults: Shared configurations for OpenTelemetry, resilience, and service discovery.


## 🚦 How to Run

### **Prerequisites**

- .NET 8.0 SDK (or newer)

- Visual Studio 2022 with MAUI and Aspire workloads installed.

- Docker Desktop (required for Aspire orchestration).

### **Execution Steps**

Clone the Repository:

```
git clone https://github.com/your-username/XFrame.git
cd XFrame
```

Set Startup Project: In Visual Studio, right-click the XFrame.AppHost project and select Set as Startup Project.

Run the Orchestrator: Press F5.

This will launch:

- The Aspire Dashboard (accessible via browser).
- The XFrame Desktop App.

Load Samples: Upon startup, the app automatically checks for sample.xml and transform.xslt to populate the editors for immediate testing.

