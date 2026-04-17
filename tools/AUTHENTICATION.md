# Screenshot Capture Tool - Authentication Fix

## Problem
The InternalAI login page (`/login`) doesn't contain a login form directly. Instead, it:
1. Redirects to the home page (`/`)
2. Opens a **login modal** component (`AdminLogin.js`)

This means traditional form-filling approaches don't work.

## Solution
The updated screenshot tool now:

1. **Navigates to `/login`** - This triggers the redirect and modal opening
2. **Waits for modal** - Uses `[data-testid='admin-login-modal']` to detect when modal appears
3. **Fills modal form** - Targets the Input components within the modal:
   - Username: `[data-testid='username-input'] input` or `input#username`
   - Password: `[data-testid='password-input'] input` or `input#password`
4. **Clicks login** - `button[data-testid='login-button']`
5. **Verifies success** - Waits for modal to close and looks for logout button

## Default Credentials
```
Username: admin
Password: Admin1234!
```

## Custom Credentials
Set via environment variables before running:
```powershell
$env:INTERNALAI_USERNAME = "your_username"
$env:INTERNALAI_PASSWORD = "your_password"
.\Capture-Screenshots.ps1
```

## Verification
The tool now properly verifies login by:
- Checking if the login modal closes (successful login closes it)
- Looking for "Logout" button in the UI
- Reporting authentication status in console output

## Troubleshooting
If screenshots still show unauthenticated pages:
1. Verify credentials work by manual login at `http://localhost:3000/login`
2. Check console output for login step details
3. Increase wait times in `Program.cs` if network is slow
4. Run tool with visible browser (change `Headless = false`) to see what's happening
