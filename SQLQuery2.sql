   SELECT 'Clientes' AS Tabla, COUNT(*) AS Filas FROM Clientes
   UNION ALL SELECT 'Productos', COUNT(*) FROM Productos
   UNION ALL SELECT 'Ventas', COUNT(*) FROM Ventas
   UNION ALL SELECT 'DetalleVenta', COUNT(*) FROM DetalleVenta;

   SELECT t.name AS Tabla, c.name AS Columna
   FROM sys.columns c
   JOIN sys.tables t ON t.object_id = c.object_id
   WHERE c.is_identity = 1;