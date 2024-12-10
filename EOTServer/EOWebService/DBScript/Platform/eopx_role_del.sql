
-- set
UPDATE eox_role SET _update_flag=-1 WHERE f_role_id = #v_role_id;
-- end

-- set
SELECT 0 AS _d, '' AS _s, #v_role_id AS f_role_id;
-- end
    