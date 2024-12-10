
---- 先删除子项

-- set
UPDATE eox_dic SET _update_flag=-1 WHERE f_dic_pid=#v_dic_id;
-- end

-- set
UPDATE eox_dic SET _update_flag=-1 WHERE f_dic_id=#v_dic_id;
-- end
    
-- set
SELECT 0 AS _d, '' AS _s, #v_dic_id AS f_dic_id;
-- end