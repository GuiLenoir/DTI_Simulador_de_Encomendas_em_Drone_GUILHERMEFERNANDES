# Demo Deploy Quick Guide

## Railway

### 1. Create MySQL

1. Open Railway.
2. Create a new project.
3. Add a MySQL service.
4. Keep the generated variables available:
   - `MYSQLHOST`
   - `MYSQLPORT`
   - `MYSQLDATABASE`
   - `MYSQLUSER`
   - `MYSQLPASSWORD`

### 2. Create Backend Service

1. In the same Railway project, add a new service from this repository.
2. Choose Docker deployment.
3. Set Dockerfile path:

```text
backend/DroneDelivery.Api/Dockerfile
```

4. Add these variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=${MYSQLHOST};Port=${MYSQLPORT};Database=${MYSQLDATABASE};User=${MYSQLUSER};Password=${MYSQLPASSWORD};SslMode=Preferred;
Cors__AllowedOrigins__0=https://your-vercel-app.vercel.app
```

5. Deploy.
6. Test:

```text
https://your-api.up.railway.app/swagger
```

## Vercel

### 1. Create Frontend Project

1. Open Vercel.
2. Import this repository.
3. Use:

```text
Framework Preset: Vite
Build Command: cd frontend && npm install && npm run build
Output Directory: frontend/dist
```

### 2. Add Environment Variable

Add:

```text
VITE_API_URL=https://your-api.up.railway.app
```

### 3. Deploy

1. Deploy the project.
2. Copy the final Vercel URL.
3. Go back to Railway.
4. Update:

```text
Cors__AllowedOrigins__0=https://your-real-vercel-url.vercel.app
```

5. Redeploy the Railway backend.

## Final Test

1. Open the Vercel URL.
2. Create an order.
3. Click to plan deliveries.
4. Check the dashboard.
5. Open the Railway Swagger URL and confirm the API responds.
