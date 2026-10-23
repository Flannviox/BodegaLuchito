-- Elimina SOLO las ventas de prueba generadas por seed-ventas-bi.sql.
-- No revierte la fecha de alta de los productos (FechaCreacion).
BEGIN TRANSACTION;
DELETE FROM DetallesVenta WHERE VentaId IN (SELECT Id FROM Ventas WHERE MotivoAnulacion LIKE 'SEED-BI#%');
DELETE FROM Ventas WHERE MotivoAnulacion LIKE 'SEED-BI#%';
COMMIT;
SELECT COUNT(*) AS VentasDePruebaRestantes FROM Ventas WHERE MotivoAnulacion LIKE 'SEED-BI#%';
