	
-- set
SELECT TA.*, TB.f_name AS f_menu_pid_s
	FROM eox_menu AS TA, eox_menu AS TB 
    WHERE TA.f_menu_pid = TB.f_menu_id
    AND TA._update_flag > 0
    ORDER BY TA.f_level, TA.f_menu_pid, TA.f_order;
-- end