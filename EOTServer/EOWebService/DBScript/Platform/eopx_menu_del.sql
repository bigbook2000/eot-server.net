
-- set
UPDATE eox_menu SET _update_flag=-1 WHERE f_menu_id=#v_menu_id;
-- end

-- set    
SELECT 0 AS _d, '' AS _s, #v_menu_id AS f_menu_id;
-- end