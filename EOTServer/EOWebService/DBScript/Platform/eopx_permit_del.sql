
-- set
UPDATE eox_permit SET _update_flag=-1 WHERE f_permit_id = #v_permit_id;
-- end

-- set    
SELECT 0 AS _d, '' AS s, #v_permit_id AS f_permit_id;
-- end
