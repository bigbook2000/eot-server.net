
-- set
UPDATE eox_dept SET _update_flag=-1 WHERE f_dept_id=#v_dept_id;
-- end

-- set
SELECT #v_dept_id AS f_dept_id;
-- end