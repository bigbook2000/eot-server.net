
-- iff > 0 #v_menu_id
	-- set
	UPDATE eox_menu SET
		f_menu_pid = #v_menu_pid,
        f_order = #v_order,
		f_level = #v_level,
		f_name = '#v_name',
		f_type = '#v_type',
		f_icon = '#v_icon',
		f_path = '#v_path',
		f_permit = '#v_permit',
		f_status = #v_status,
		_update_time = ##now
		WHERE f_menu_id = #v_menu_id;
	-- end
-- end

-- iff <= 0 #v_menu_id
	-- inc #v_menu_id
	INSERT INTO eox_menu(
		f_menu_pid,
        f_order,
		f_level,
		f_name,
		f_type,
		f_icon,
		f_path,
		f_permit,
		f_status,
		_update_time,
		_update_flag)
        VALUES(
        #v_menu_pid,
        #v_order,
		#v_level,
		'#v_name',
		'#v_type',
		'#v_icon',
		'#v_path',
		'#v_permit',
		#v_status,
		##now, 1);            
	-- end
-- end
	
-- set
SELECT TA.*, TB.f_name AS f_menu_pid_s
	FROM eox_menu AS TA, eox_menu AS TB 
    WHERE TA.f_menu_pid = TB.f_menu_id
    AND TA.f_menu_id = #v_menu_id;
-- end