# Setting up the database

## Create the database

Log into SQL Server Express

Select SQLEXPRESS > Database > New Database...

![New Database... menu item](doc/image/Image2.png)

Database name: pbx-dev

![New Database dialog](doc/image/Image3.png)

## Create the tblPbxLog table

Select SQLEXPRESS > Database > pbx-dev > New Query...

![New Query... menu item](doc/image/Image4.png)

Paste in the contents of [something.sql](something.sql)
Click 'Execute'

![Paste SQL and Execute](doc/image/Image6.png)


## Enable username/password logins

Select SQLEXPRESS > Properties

![Properties menu item](doc/image/Image7.png)

Select 'Security' on the left hand side, then
Select 'SQL Server and Windows Authentication mode'
Click OK

![Server Properties dialog](doc/image/Image8.png)

Select SQLEXPRESS > Restart

![Restart menu item](doc/image/Image9.png)

## Create user

Select SQLEXPRESS > Security > Logins > New Login... 

![New Login... menu item](doc/image/Image10.png)

Select 'General' on the left hand side, then
Login name: pbx-dev-username
SQL Server authentication
Password: pbx-dev-password
Confirm password: pbx-dev-password
(You probably want to set your own username and password here)

![New Login - General dialog](doc/image/Image11.png)

Select 'User Mapping' on the left hand side, then
Users mapped to this login: check pbx-dev 
Database role membership for pbx-dev: check db_owner
Click OK

![New Login - User Mapping dialog](doc/image/Image11.png)


## Enable protocols for username/password logins

Run Sql Server Configuration Manager
Select SQL Server Network Configuration > Protocols for SQLEXPRESS
Select TCP/IP > Enabled
Select Named Pipes > Enabled

![Sql Server Configuration Manager - Protocols](doc/image/Image11.png)

Select SQL Server Services > SQLEXPRESS > Restart

![Sql Server Configuration Manager - Restart service](doc/image/Image11.png)

## Test login

Reopen SQL Server Management Studio
Check that the new user login works
Authentication: SQL Server Authentication
Login: pbx-dev-username
Password: pbx-dev-password
Click Connect

![Login](doc/image/Image11.png)

If that all seem to work, then you should be able to set up your connection congfiguration in the registry and start the service

[Return to README.md](README.md)
