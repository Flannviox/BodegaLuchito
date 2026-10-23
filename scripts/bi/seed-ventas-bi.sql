-- =====================================================================
-- Bodega Luchito · Ventas de PRUEBA para el módulo de Predicción de demanda
-- Genera 10 semanas completas de ventas: lunes 10/08/2026 a domingo 18/10/2026.
-- USAR SOLO EN UNA COPIA DE PRUEBA DE LA BASE (ver instrucciones).
-- Las ventas generadas quedan marcadas con MotivoAnulacion = 'SEED-BI#...'
-- (con Anulado = 0) para poder borrarlas luego con limpiar-ventas-bi.sql.
-- Si el script ya se ejecutó, no vuelve a insertar nada.
-- =====================================================================
BEGIN TRANSACTION;

-- 1) El historial de cada producto empieza el día siguiente a su alta.
--    Se adelanta el alta para que las 10 semanas cuenten como historial.
UPDATE Producto
SET FechaCreacion = '2026-08-01 08:00:00'
WHERE Activo = 1 AND FechaCreacion > '2026-08-01 08:00:00'
  AND NOT EXISTS (SELECT 1 FROM Ventas WHERE MotivoAnulacion LIKE 'SEED-BI#%');

-- 2) Cantidades diarias por producto con un patrón semanal claro:
--    base según el producto, +30 % los viernes, +60 % sábados y domingos,
--    leve tendencia al alza, pequeña variación y algunos días sin venta.
DROP TABLE IF EXISTS temp.seed_bi;
CREATE TEMP TABLE seed_bi AS
WITH RECURSIVE dias(n, fecha) AS (
    SELECT 0, date('2026-08-10')
    UNION ALL
    SELECT n + 1, date(fecha, '+1 day') FROM dias WHERE n < 69
),
base AS (
    SELECT p.Id AS ProductoId, p.UnidadVenta, d.n, d.fecha,
           MAX(CAST(p.PrecioVenta AS REAL), 0.5) AS Precio,
           CASE CAST(strftime('%w', d.fecha) AS INTEGER)
                WHEN 0 THEN 1.6 WHEN 6 THEN 1.6 WHEN 5 THEN 1.3 ELSE 1.0 END AS FactorDia,
           ((p.Id * 7 + d.n * 13) % 3) - 1 AS Ruido
    FROM dias d CROSS JOIN Producto p
    WHERE p.Activo = 1
      AND (p.Id + d.n) % 11 <> 0                 -- algunos días sin ventas
)
SELECT ProductoId, fecha, n, Precio,
       CASE WHEN UnidadVenta IN ('Peso', '2')
            THEN ROUND(MAX((0.75 + (ProductoId % 4) * 0.25) * FactorDia * (1 + n * 0.004) + Ruido * 0.125, 0.125), 3)
            ELSE MAX(ROUND((2 + (ProductoId % 5)) * FactorDia * (1 + n * 0.004) + Ruido), 1)
       END AS Cantidad
FROM base;

-- 3) Una venta por producto y día (pagos rotando entre Efectivo=1, Yape=2, Plin=3).
INSERT INTO Ventas (UsuarioId, FechaHora, Subtotal, IGV, Total, MetodoPago, Anulado, MotivoAnulacion)
SELECT (SELECT Id FROM Usuario ORDER BY Id LIMIT 1),
       printf('%s %02d:%02d:00', s.fecha, 8 + (s.ProductoId + s.n) % 12, (s.ProductoId * 7 + s.n) % 60),
       ROUND(ROUND(s.Cantidad * s.Precio, 2) / 1.18, 2),
       ROUND(ROUND(s.Cantidad * s.Precio, 2) - ROUND(ROUND(s.Cantidad * s.Precio, 2) / 1.18, 2), 2),
       ROUND(s.Cantidad * s.Precio, 2),
       1 + (s.ProductoId + s.n) % 3,
       0,
       'SEED-BI#' || s.ProductoId || '#' || s.fecha
FROM seed_bi s
WHERE NOT EXISTS (SELECT 1 FROM Ventas WHERE MotivoAnulacion LIKE 'SEED-BI#%');

-- 4) Detalle de cada venta generada (solo las que aún no tienen detalle).
INSERT INTO DetallesVenta (VentaId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
SELECT v.Id, s.ProductoId, s.Cantidad, s.Precio, ROUND(s.Cantidad * s.Precio, 2)
FROM seed_bi s
JOIN Ventas v ON v.MotivoAnulacion = 'SEED-BI#' || s.ProductoId || '#' || s.fecha
WHERE NOT EXISTS (SELECT 1 FROM DetallesVenta d WHERE d.VentaId = v.Id);

DROP TABLE temp.seed_bi;
COMMIT;

-- Verificación rápida: ventas generadas por semana.
SELECT date(FechaHora, 'weekday 0', '-6 days') AS Lunes, COUNT(*) AS Ventas, ROUND(SUM(Total), 2) AS Importe
FROM Ventas WHERE MotivoAnulacion LIKE 'SEED-BI#%'
GROUP BY 1 ORDER BY 1;
