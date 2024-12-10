
-- set
UPDATE eox_setting SET f_value=v_value WHERE f_key=#v_key;
-- end

-- set
select 0 AS _d, '' AS _s, eox_setting.* FROM eox_setting WHERE f_key=#v_key;
-- end