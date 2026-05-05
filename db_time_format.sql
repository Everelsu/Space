-- ============================================================
-- Скрипт настройки БД для формата времени чч:мм:сс
-- ============================================================
-- Этот скрипт НЕ изменяет существующие данные, а только документирует
-- что поле hours_spent хранится как DECIMAL (часы в десятичном формате)
-- 
-- Пример: 1.5 часа = 1 час 30 минут
--         2.75 часа = 2 часа 45 минут
--
-- Отображение в формате чч:мм:сс происходит на уровне UI (C# код)
-- ============================================================

-- Проверка текущей структуры таблицы worklogs
-- Поле hours_spent должно быть типа DECIMAL/NUMERIC
SELECT column_name, data_type, numeric_precision, numeric_scale
FROM information_schema.columns
WHERE table_name = 'worklogs' AND column_name = 'hours_spent';

-- Если нужно изменить тип (обычно не требуется):
-- ALTER TABLE worklogs ALTER COLUMN hours_spent TYPE DECIMAL(10,2);

-- Пример выборки данных с конвертацией в формат чч:мм:сс на уровне SQL:
-- SELECT 
--     id,
--     task_id,
--     employee_id,
--     log_date,
--     hours_spent,
--     comment,
--     -- Конвертация decimal часов в формат чч:мм:сс
--     CONCAT(
--         LPAD(FLOOR(hours_spent)::TEXT, 2, '0'), ':',
--         LPAD(FLOOR((hours_spent - FLOOR(hours_spent)) * 60)::TEXT, 2, '0'), ':',
--         LPAD(FLOOR(((hours_spent - FLOOR(hours_spent)) * 60 - FLOOR((hours_spent - FLOOR(hours_spent)) * 60)) * 60)::TEXT, 2, '0')
--     ) AS time_formatted
-- FROM worklogs;

-- ============================================================
-- Примечание: В текущей реализации C# приложения:
-- - TimerService.ElapsedText возвращает строку в формате "чч:мм:сс"
-- - WorkLogItem.Hours хранится как decimal (десятичные часы)
-- - При сохранении в БД используется decimal значение
-- - При отображении в UI можно форматировать как угодно
-- ============================================================
