# GQI Monitor

A low-code app that allows you to monitor and analyze the performance of GQI queries on the system including:
- The number of queries executed
- The number of active users
- The maximum and average duration of queries

Currently, the app offers 5 distinct pages.
- __Live monitor__: metrics of the last 15 minutes updating every 10 seconds
- __Metric history__: overview of all metrics available on the system
- __Application metrics__: overview of the top 10 applications for each metric
- __User metrics__: overview of the top 10 users for each metric
- __Extension logs__: search and inspect log entries for each extension library  
 *Prerequisites: web version 10.5.9 or higher with GQI DxM*
- __Ad hoc data source metrics__: overview of performance and usage metrics for each _active_ ad hoc data source on the system.  
 *Prerequisites: web version 10.5.9 or higher with GQI DxM*

![Screenshot](./Images/screenshot.png)