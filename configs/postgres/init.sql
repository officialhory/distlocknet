CREATE USER admin;
ALTER USER admin WITH SUPERUSER;

CREATE DATABASE locker;
GRANT ALL PRIVILEGES ON DATABASE locker TO admin;