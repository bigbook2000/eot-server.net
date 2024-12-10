
-- iff > 0 #v_dept_id

	-- set
	UPDATE eox_dept SET
		f_dept_pid = #v_dept_pid,
		f_level = #v_level,
		f_name = '#v_name',		
		f_contact = '#v_contact',
		f_phone = '#v_phone',
		f_note = '#v_note',
		f_status = '#v_status',
		f_data_ex = '#v_data_ex',
		_update_time = ##now
		WHERE f_dept_id = #v_dept_id;
	-- end

-- end

-- iff <= 0 #v_dept_id
	
	---- 自增变量
	-- inc #v_dept_id
	INSERT INTO eox_dept(
		f_dept_pid,
		f_level,
		f_name,
		f_contact,
		f_phone,
		f_note,
		f_status,
		f_data_ex,
		_update_time,
		_update_flag)
        VALUES (
        #v_dept_pid,
		#v_level,
		'#v_name',
		'#v_contact',
		'#v_phone',
		'#v_note',
		#v_status,
		'#v_data_ex',
		##now,
		1);
	-- end

-- end

-- set
SELECT eox_dept.* FROM eox_dept WHERE f_dept_id = #v_dept_id;
-- end