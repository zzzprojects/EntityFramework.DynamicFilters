---
permalink: trial-period-expired-exception
---

## Problem

You execute a method from the Entity Framework Extensions library, and the following error is thrown:

{% include template-exception.html message='ERROR_005: The monthly trial period is expired. You can extend your trial by downloading the latest version. You can also purchase a perpetual license on our website. If you already own this license, this error only appears if the license has not been found, you can find additional information on our troubleshooting section (http://entityframework-extensions.net/troubleshooting). Contact our support team for more details: info@zzzprojects.com' %}

## Solution

### Cause

- You are currently evaluating the library and the trial period is expired.
- You have purchased the license but didn't register it correctly.

### Fix

#### Trial Period Expired

You can extend your trial by downloading the latest version: [Upgrading](http://entityframework-extensions.net/upgrading)

The latest version always contains a trial for the current month to allows a company to evaluate our library for several months.

#### License Badly Registered

Make sure to follow all recommendation about how to setup your license: [Licensing](http://entityframework-extensions.net/licensing)

Otherwise contact us: {% include infozzzprojects-email.html %}
