# AspNetFormsAuth

AspNetFormsAuth is a plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of **ASP.NET forms authentication** to Rhetos server applications.

The authentication is implemented using Microsoft's *WebMatrix SimpleMembershipProvider*,
with recommended security best practices such as password salting and hashing.
Implementation fully depends on SimpleMembershipProvider; AspNetFormsAuth project does not try
to implement its own authentication or security mechanisms.

Table of contents:

1. [Features](#features)
   1. [Authentication](#authentication)
   2. [Common administration activities](#common-administration-activities)
   3. [Forgot password](#forgot-password)
   4. [Simple administration GUI](#simple-administration-gui)
2. [Authentication service API](#authentication-service-api)
   1. [Login](#login)
   2. [Logout](#logout)
   3. [SetPassword](#setpassword)
   4. [ChangeMyPassword](#changemypassword)
   5. [UnlockUser](#unlockuser)
   6. [GeneratePasswordResetToken](#generatepasswordresettoken)
   7. [SendPasswordResetToken](#sendpasswordresettoken)
   8. [ResetPassword](#resetpassword)
3. [Installation](#installation)
   1. [1. Modify Web.config](#1-modify-webconfig)
   2. [2. Configure IIS](#2-configure-iis)
   3. [3. Configure IIS Express](#3-configure-iis-express)
   4. [4. Set up HTTPS](#4-set-up-https)
4. [Configuration](#configuration)
   1. [Set the *admin* user password](#set-the-admin-user-password)
   2. [Permissions and claims](#permissions-and-claims)
   3. [Maximum failed password attempts](#maximum-failed-password-attempts)
   4. [Password strength policy](#password-strength-policy)
   5. [Overriding IIS binding configuration](#overriding-iis-binding-configuration)
5. [Uninstallation](#uninstallation)
   1. [Modify Web.config](#modify-webconfig)
   2. [Configure IIS](#configure-iis)
6. [Sharing the authentication across web applications](#sharing-the-authentication-across-web-applications)
7. [Session timeout](#session-timeout)
8. [Implementing SendPasswordResetToken](#implementing-sendpasswordresettoken)
   1. [Custom implementation](#custom-implementation)
9. [Troubleshooting](#troubleshooting)
10. [Build](#build)

## Features

### Authentication

* For developers and administrators, a simple login and logout web forms are provided.
  Links are available on the Rhetos server home page.
* [Authentication service](#authentication-service-api) may be used in web applications
  and other services to log in and log out users, and for other related actions.
* Forms authentication may be utilized for [sharing the authentication](#sharing-the-authentication-across-web-applications)
  across multiple web applications.

### Common administration activities

* To create a new user, insert the record in the `Common.Principal` entity.
* To configure the user's permissions, enter the data in `Common.PrincipalHasRole` or `Common.PrincipalPermission`.
* To set the user's password, the administrator may use [`SetPassword`](#setpassword)
  or [`GeneratePasswordResetToken`](#generatepasswordresettoken)  web service methods (see below).
  The user will later use [`ResetPassword`](#resetpassword) with the password reset token,
  or [`ChangeMyPassword`](#changemypassword) when logged-in.

### Forgot password

There are two recommended ways of implementing *forgot password* functionality with AspNetFormsAuth:

* Option 1: An administrator (or a web application *with administrator privileges*) may call
  [`GeneratePasswordResetToken`](#generatepasswordresettoken) web method to get the user's password reset token.
  The administrator or the web application should then send the token to the user on its own.

* Option 2: An end user that is not logged-in (or a web application *with no special privileges*) may call
  [`SendPasswordResetToken`](#sendpasswordresettoken) web method. The Rhetos sever will generate
  the password reset token and send it to the user. In order to use this method,
  an implementation of sending the token (by SMS or email, e.g.) should be provided by an additional plugin
  (see [Implementing SendPasswordResetToken](#implementing-sendpasswordresettoken)).

### Simple administration GUI

For testing and administration, a simple web GUI is available at the Rhetos server homepage under *AspNetFormsAuth* header.

## Authentication service API

The JSON service is available at URI `<rhetos server>/Resources/AspNetFormsAuth/Authentication`, with the following methods.

### Login

* Interface: `(string UserName, string Password, bool PersistCookie) -> bool`
* Example of the request data: `{"UserName":"myusername","Password":"mypassword","PersistCookie":false}`.
* The method does not require user authentication.
* On successful log in, the server response will contain the standard authentication cookie.
  The client browser will automatically use the cookie for following requests.
* Response data is boolean *true* if the login is successful,
  *false* if the login and password does not match,
  or an error message (string) with HTTP error code 4* or 5* in case of any other error.

### Logout

* No request data is needed, assuming standard authentication cookie is automatically provided. Response is empty.

### SetPassword

Sets or resets the given user's password.

* Interface: `(string UserName, string Password, bool IgnorePasswordStrengthPolicy) -> void`
* Requires `SetPassword` [security claim](#permissions-and-claims).
  If IgnorePasswordStrengthPolicy property is set, `IgnorePasswordStrengthPolicy` [security claim](#permissions-and-claims) is required.
* Response data is empty if the command is successful,
  an error message (string) with HTTP error code 400 if the password does not match the password strength policy,
  or an error message with HTTP error code 4* or 5* in case of any other error.

### ChangeMyPassword

Changes the current user's password.

* Interface: `(string OldPassword, string NewPassword) -> bool`
* Response data is boolean *true* if the login is successful,
  *false* if the login and password does not match,
  an error message (string) with HTTP error code 400 if the password does not match the password strength policy,
  or an error message with HTTP error code 4* or 5* in case of any other error.

### UnlockUser

Reset the number of [failed login attempts](#maximum-failed-password-attempts).

* Interface: `(string UserName) -> void`
* Response is empty.
* Requires `UnlockUser` [security claim](#permissions-and-claims).

### GeneratePasswordResetToken

Generates a password reset token.

* Interface: `(string UserName, int TokenExpirationInMinutesFromNow) -> string`
* This method is typically called by an administrator or a web application with administrator privileges
  in order to create a user account without initial password and let a user choose it, or to implement forgot-password functionality.
* To implement forgot-password functionality *without* using administrator privileges in web application,
  use [`SendPasswordResetToken`](#sendpasswordresettoken) method instead (see [Forgot password](#forgot-password)).
* Requires `GeneratePasswordResetToken` [security claim](#permissions-and-claims).
* If TokenExpirationInMinutesFromNow parameter is not set (or set to 0), the token will expire in 24 hours.

### SendPasswordResetToken

Generates a password reset token and sends it to the user.

* Interface: `(string UserName, Dictionary<string, string> AdditionalClientInfo) -> void`
* When using this method there is no need to directly call [`GeneratePasswordResetToken`](#generatepasswordresettoken)
  method (see [Forgot password](#forgot-password)).
* The method does not require user authentication.
* **NOTE:** *AspNetFormsAuth* package **does not contain** any implementation of sending  the token (by SMS or email, e.g.).
  The implementation must be provided by an additional plugin. For example:
  * Use the [SimpleSPRTEmail](https://github.com/Rhetos/SimpleSPRTEmail) plugin package for sending token by email,
  * or follow [Implementing SendPasswordResetToken](#implementing-sendpasswordresettoken) to implement a different sending method.
* Use `AspNetFormsAuth.SendPasswordResetToken.ExpirationInMinutes` appSettings key in `web.config` to set the token expiration timeout.
  Default value is 1440 minutes (24 hours).
  For example: `<add key="AspNetFormsAuth.SendPasswordResetToken.ExpirationInMinutes" value="60" />`.

### ResetPassword

Allows a user to set the initial password or reset the forgotten password, using the token he received previously.

* Interface: `(string PasswordResetToken, string NewPassword) -> bool`
* See `GeneratePasswordResetToken` method for *PasswordResetToken*.
* The method does not require user authentication.
* Response data is boolean *true* if the password change is successful,
  *false* if the token is invalid or expired,
  or an error message (string) with HTTP error code 4* or 5* in case of any other error.

## Installation

To install this package to a Rhetos server, add it to the Rhetos server's *RhetosPackages.config* file
and make sure the NuGet package location is listed in the *RhetosPackageSources.config* file.

* The package ID is "**Rhetos.AspNetFormsAuth**".
  This package is available at the [NuGet.org](https://www.nuget.org/) online gallery.
  The Rhetos server can install the package directly from there, if the gallery is listed in *RhetosPackageSources.config* file.
* For more information, see [Installing plugin packages](https://github.com/Rhetos/Rhetos/wiki/Installing-plugin-packages).

Before or after deploying the AspNetFormsAuth packages, please make the following changes to the web site configuration,
in order for the forms authentication to work:

### 1. Modify Web.config

1. Comment out or delete the following **two occurrences** of the `security` element:

    ```XML
    <security mode="TransportCredentialOnly">
      <transport clientCredentialType="Windows" />
    </security>
    ...
    <security mode="TransportCredentialOnly">
      <transport clientCredentialType="Windows" />
    </security>
    ```

2. Remove the `<authentication mode="Windows" />` element.
3. Inside the `<system.web>` element add the following:

    ```XML
    <authentication mode="Forms" />
    <roleManager enabled="true" />
    <membership defaultProvider="SimpleMembershipProvider">
      <providers>
        <clear />
        <add name="SimpleMembershipProvider" type="WebMatrix.WebData.SimpleMembershipProvider, WebMatrix.WebData" />
      </providers>
    </membership>
    <authorization>
      <deny users="?" />
    </authorization>
    ```

### 2. Configure IIS

1. Start IIS Manager -> Select the web site -> Open "Authentication" feature.
2. On the Authentication page **enable** *Anonymous Authentication* and *Forms Authentication*,
   **disable** *Windows Authentication* and every other.
3. Allow IIS system accounts read access to the Rhetos server folder and write access to the Rhetos logs folder (the "Logs" subfolder or directly in the Rhetos server folder, depending on the settings in *web.config*), by entering these commands to the command prompt *as administrator*, in the "RhetosServer" folder:

        ICACLS . /grant "BUILTIN\IIS_IUSRS":(OI)(CI)(RX)
        ICACLS . /grant "NT AUTHORITY\IUSR":(OI)(CI)(RX)
        IF NOT EXIST Logs\ MD Logs
        ICACLS .\Logs /grant "BUILTIN\IIS_IUSRS":(OI)(CI)(M)
        ICACLS .\Logs /grant "NT AUTHORITY\IUSR":(OI)(CI)(M)

### 3. Configure IIS Express

Note: *(Only if using IIS Express instead of IIS server)*

If using IIS Express, after deploying AspNetFormsAuth package, execute `SetupRhetosServer.bat`
utility in Rhetos server's folder to automatically configure `IISExpress.config`,
or manually apply the following lines in IISExpress configuration file inside `system.webServer` element
or inside `location / system.webServer` (usually at the end of the file):

```XML
<security>
    <authentication>
        <anonymousAuthentication enabled="false" />
        <windowsAuthentication enabled="true" />
    </authentication>
</security>
```

### 4. Set up HTTPS

HTTPS (or any other) secure transport protocol **should always be enforced** when using forms authentication.
This is necessary because in forms authentication the **user's password** must be submitted from the client securely.
At least the services inside `/Resources/AspNetFormsAuth` path must use HTTPS.

To enable HTTPS, follow the instructions in [Setting up Rhetos for HTTPS](https://github.com/Rhetos/Rhetos/wiki/Setting-up-Rhetos-for-HTTPS).

Consider using a [free SSL certificate](https://www.google.hr/search?q=free+SSL+certificate)
in development or QA environment.

## Configuration

### Set the *admin* user password

Note: When deploying the AspNetFormsAuth packages, it will automatically create
the *admin* user account and *SecurityAdministrator* role, add the account to the role
and give it necessary permissions (claims) for all authentication service methods.

After deployment:

1. Run the Rhetos utility `bin\Plugins\AdminSetup.exe` to initialize the *admin* user's password.

### Permissions and claims

All claims related to the authentication service have resource=`AspNetFormsAuth.AuthenticationService`.
[Admin user](#admin-user) has all the necessary permissions (claims) for all authentication service methods.

### Maximum failed password attempts

Use entity *Common.AspNetFormsAuthPasswordAttemptsLimit* (*MaxInvalidPasswordAttempts*, *TimeoutInSeconds*)
to configure automatic account locking when a number of failed password attempts is reached.

* When *MaxInvalidPasswordAttempts* limit is passed, the user's account is temporarily locked.
* If *TimeoutInSeconds* is set, user's account will be temporarily locked until the specified time period has passed.
  If the value is not set or 0, the account will be locked permanently.
* Administrator may use [`UnlockUser`](#unlockuser) method to unlock the account, or wait for *TimeoutInSeconds*.
* Multiple limits may be entered. An example with two entries:
  * After 3 failed attempts, the account is temporarily locked for 120 seconds.
  * After 10 failed attempts, the account is locked until *admin* unlocks it manually (timeout=0).

### Password strength policy

Use entity *Common.AspNetFormsAuthPasswordStrength* (*RegularExpression*, *RuleDescription*) to configure the policy.

* A new password must pass all the rules in *Common.AspNetFormsAuthPasswordStrength*.
* *RuleDescription* is uses as an error message to the user if the new password breaks the policy.
* When administrator executes [`SetPassword`](#setpassword) method, the property *IgnorePasswordStrengthPolicy*
  may be used to avoid the policy.

Examples:

RegularExpression|RuleDescription
-----------------|---------------
`.{6,}`          | The password length must be at least six characters.
`\d`             | The password must contain at least one digit.
`(\d.*){3,}`     | The password must contain at least three digits.
`[A-Z]`          | The password must contain at least one uppercase letters.
`\W`             | The password must contain at least one special character (not a letter or a digit).

### Overriding IIS binding configuration

WebServiceHost will automatically create HTTP and HTTPS REST-like endpoint/binding/behavior pairs if service endpoint/binding/behavior configuration is empty.

If you need to override default behavior (i.e. enable only HTTPS), you need to add following in `services` section:

```XML
<service name="Rhetos.AspNetFormsAuth.AuthenticationService">
  <clear />
  <endpoint binding="webHttpBinding" bindingConfiguration="rhetosWebHttpsBinding" contract="Rhetos.AspNetFormsAuth.AuthenticationService" />
</service>
```

Also, you need to define new `webHttpBinding` `binding` item:

```XML
<binding name="rhetosWebHttpsBinding" maxReceivedMessageSize="209715200">
  <security mode="Transport" />
  <readerQuotas maxArrayLength="209715200" maxStringContentLength="209715200" />
</binding>
```

## Uninstallation

When returning Rhetos server from Forms Authentication back to **Windows Authentication**, the following configuration changes should be done:

### Modify Web.config

1. Add (or uncomment) the following element inside all `<binding ...>` elements:

    ```XML
    <security mode="TransportCredentialOnly">
        <transport clientCredentialType="Windows" />
    </security>
    ```

2. Inside `<system.web>` remove following elements:

    ```XML
    <authentication mode="Forms" />
    <roleManager enabled="true" />
    <membership defaultProvider="SimpleMembershipProvider">
      <providers>
        <clear />
        <add name="SimpleMembershipProvider" type="WebMatrix.WebData.SimpleMembershipProvider, WebMatrix.WebData" />
      </providers>
    </membership>
    <authorization>
      <deny users="?" />
    </authorization>
    ```

3. Inside `<system.web>` add the `<authentication mode="Windows" />` element.

### Configure IIS

1. Start IIS Manager -> Select the web site -> Open "Authentication" feature.
2. On the Authentication page **disable** *Anonymous Authentication* and *Forms Authentication*, **enable** *Windows Authentication*.

## Sharing the authentication across web applications

Sharing the authentication cookie is useful when using separate web applications for web pages and application services, or when using multiple servers for load balancing.
In these scenarios, sharing the forms authentication cookie between the sites will allow a single-point login for the user on any of the sites and seamless use of that cookie on the other sites.

In most cases, for the web applications to share the authentication cookie, it is enough to have the **same** `machineKey` element configuration in the `web.config`.
For more background info, see [MSDN article: Forms Authentication Across Applications](http://msdn.microsoft.com/en-us/library/eb0zx8fc.aspx).

Steps:

1. Generate a new machine key:
    * Select *Validation method*: HMACSHA256, HMACSHA384, or HMACSHA512 (SHA1, MD5 and 3DES are obsolete).
    * Select *Encryption method*: AES (DES and 3DES are obsolete).
    * See [how to](https://www.codeproject.com/Articles/221889/How-to-Generate-Machine-Key-in-IIS).
2. Copy the machine key values to other web applications in the same deployment environment (using the IIS Manager, or copy the `machineKey` element in *web.config*).

For security reasons, it is important to generate the new validationKey and decryptionKey **for each deployment environment**.
If you have multiple Rhetos applications on a single server and do not want to share the authentication between them, make sure to generate different machine keys.

## Session timeout

ASP.NET forms authentication ticket will expire after 30 minutes of **client inactivity**, by default.
To allow user to stay logged in after longer time of inactivity, add standard [ASP.NET configuration](https://msdn.microsoft.com/en-us/library/1d3t3c61(v=vs.100).aspx) option `timeout` (in minutes) in Web.config:

```XML
<system.web>
     <authentication mode="Forms">
       <forms timeout="50000000"/>
     </authentication>
</system.web>
```

## Implementing SendPasswordResetToken

In order to use [`SendPasswordResetToken`](#sendpasswordresettoken) web method (see also [Forgot password](#forgot-password)),
an additional plugin must be provided that sends the token to the user (by SMS or email, e.g.).

* A sample implementation is available at [https://github.com/Rhetos/SimpleSPRTEmail](https://github.com/Rhetos/SimpleSPRTEmail).
  This plugin package may be used for sending simple emails.

### Custom implementation

In order to implement a custom method of sending the token to the user (by SMS or email, e.g.),
create a Rhetos plugin package with a class that implements the `Rhetos.AspNetFormsAuth.ISendPasswordResetToken` interface
from `Rhetos.AspNetFormsAuth.Interfaces.dll`.
The class must use `Export` attribute to register the plugin implementation.
For example:

```C#
[Export(typeof(ISendPasswordResetToken))]
public class EmailSender : ISendPasswordResetToken
{
    ...
}
```

The `AdditionalClientInfo` parameter of web service method `/SendPasswordResetToken` will be provided to the implementation function.
The parameter may contain answers to security questions, preferred method of communication or any similar user provided information
required by the `ISendPasswordResetToken` implementation.

The implementation class may throw a `Rhetos.UserException` or a `Rhetos.ClientException` to provide an error message to the client,
but use it with caution, or better avoid it: The `SendPasswordResetToken` web service method allows **anonymous access**,
so providing any error information to the client might be a security issue.

Any other exception (`Rhetos.FrameworkException`, e.g.) will only be logged on the server, but no error will be sent to the client.

## Troubleshooting

**Issue**: Deployment results with error message "DslSyntaxException: Concept with same key is described twice with different values."<br>
**Solution**: Please check if you have deployed both *SimpleWindowsAuth* package and *AspNetFormsAuth* package at the same time. Only one of the packages can be deployed on Rhetos server. Read the [installation](#installation) instructions above for more information on the issue.

**Issue**: Web service responds with error message "The Role Manager feature has not been enabled."<br>
**Solution**: The error occurs when the necessary modifications of Web.config file are not done. Please check that you have followed the [installation](#installation) instructions above.

**Issue**: I have accidentally deleted the *admin* user, *SecurityAdministrator* role, or some of its permissions. How can I get it back?<br>
**Solution**: Execute `AdminSetup.exe` again. It will regenerate the default administration settings. See [admin user](#admin-user).

**Other:** In case of a server error, additional information on the error may be found in the Rhetos server log (`RhetosServer.log` file, by default).
If needed, more verbose logging of the authentication service may be switched on by adding `<logger name="AspNetFormsAuth.AuthenticationService" minLevel="Trace" writeTo="TraceLog" />` in Rhetos server's `web.config`. The trace log will be written to `RhetosServerTrace.log`.

## Build

**Note:** This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
You don't need to build it from source in order to use it in your application.

To build the package from source, run `Build.bat`.
The script will pause in case of an error.
The build output is a NuGet package in the "Install" subfolder.
