# Oracle Connection Troubleshooting Guide

## Error: ORA-50201: Failed to connect to server or failed to parse connect string

This error means the connection string format is incorrect or the server cannot be reached.

## Quick Fix Steps

### Step 1: Find Your Correct Connection Details

You need to know:
1. **Hostname or IP**: `DbServer1` (or actual IP like `192.168.1.100`)
2. **Port**: Usually `1521` (default Oracle port)
3. **Service Name or SID**: Examples: `ORCL`, `XE`, `XEPDB1`, or your database name

**How to find these:**
- Check with your DBA
- Look at existing SQL Developer connection
- Check TNSNAMES.ORA file (usually in `ORACLE_HOME/network/admin/`)
- Ask colleague who can connect successfully

### Step 2: Run the Connection Test Program

I've created a test program that will try different connection string formats:

```bash
cd /home/user/ReportsMakerAITool
dotnet run --project TestOracleConnection.csproj
```

This will test 6 different connection string formats and tell you which one works!

### Step 3: Update appsettings.json

Once you find the working format, update:
`AIReportsTool.API/appsettings.json`

## Common Connection String Formats

### Format 1: EZ Connect (Simplest)
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=DbServer1:1521/ORCL"
```

Replace:
- `DbServer1` with your hostname or IP
- `1521` with your port
- `ORCL` with your service name

### Format 2: Using IP Address
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=192.168.1.100:1521/ORCL"
```

### Format 3: Full TNS Descriptor with SERVICE_NAME
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=DbServer1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)))"
```

### Format 4: Full TNS Descriptor with SID
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=DbServer1)(PORT=1521))(CONNECT_DATA=(SID=ORCL)))"
```

### Format 5: Using TNS Alias (if TNSNAMES.ORA is configured)
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=YOUR_TNS_ALIAS"
```

## Special Characters in Password

Your password `Pa$M0r$D16$#K` contains special characters. In most cases, this is fine, but if you continue having issues, you can try:

1. **URL Encoding** (usually not needed for Oracle):
   ```
   Password=Pa%24M0r%24D16%24%23K
   ```

2. **Escaping in JSON** (only if using quotes in password):
   Not needed for your password

## Troubleshooting Checklist

- [ ] **Can you ping the server?**
  ```bash
  ping DbServer1
  ```

- [ ] **Is the Oracle Listener running?**
  ```bash
  telnet DbServer1 1521
  ```
  (Press Ctrl+C to exit if it connects)

- [ ] **Do you have TNSNAMES.ORA?**
  Check: `$ORACLE_HOME/network/admin/tnsnames.ora`
  If yes, use the alias from that file

- [ ] **Is it a local database?**
  Try: `localhost` or `127.0.0.1` instead of `DbServer1`

- [ ] **Different port?**
  Common ports: `1521`, `1522`, `1526`, `1529`

- [ ] **Service Name vs SID?**
  Try both formats (see Format 3 and 4 above)

## Test Connection Manually

If you have Oracle client installed, test from command line:

```bash
sqlplus e1241/Pa\$M0r\$D16\$\#K@DbServer1:1521/ORCL
```

Note: Escape special characters with `\` in bash

## Common Service Names by Oracle Version

- **Oracle 11g/12c**: `ORCL`, `orcl`
- **Oracle XE (Express Edition)**: `XE`
- **Oracle 18c+ XE**: `XEPDB1`
- **Custom installations**: Ask your DBA

## If Nothing Works

Contact your DBA with these questions:
1. What is the exact hostname or IP?
2. What port is the Oracle Listener on?
3. What is the SERVICE_NAME or SID?
4. Is there a TNSNAMES.ORA entry I should use?
5. Are there any firewall rules blocking my connection?
6. Is my user `e1241` allowed to connect remotely?

## Quick Examples by Scenario

### Scenario A: Local Oracle XE Installation
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=localhost:1521/XE"
```

### Scenario B: Remote Oracle 12c Server
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=192.168.1.50:1521/ORCL"
```

### Scenario C: Oracle 18c+ XE Remote
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=DbServer1:1521/XEPDB1"
```

### Scenario D: Using TNS Name
```json
"OracleConnection": "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=PROD_DB"
```
(Where `PROD_DB` is defined in TNSNAMES.ORA)

## Need Help Right Now?

Run the test program:
```bash
cd /home/user/ReportsMakerAITool
dotnet run --project TestOracleConnection.csproj
```

It will automatically try the most common formats and tell you which one works!

---

**After you find the working connection string, update it in:**
`AIReportsTool.API/appsettings.json`

Then restart your API application.
