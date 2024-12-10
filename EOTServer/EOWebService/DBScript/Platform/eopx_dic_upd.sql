

-- iff > 0 #v_dic_id
	
	-- set
	UPDATE eox_dic SET
		f_dic_pid = #v_dic_pid, 
		f_level = #v_level, 
		f_label = '#v_label', 
		f_value = #v_value, 
		f_note = '#v_note',
		_update_time = ##now
		WHERE f_dic_id = #v_dic_id;
	-- end
-- end

-- iff <= 0 #v_dic_id

	-- inc #v_dic_id
	INSERT INTO eox_dic(
		f_dic_pid, 
		f_level,
		f_label,
		f_value,
		f_note,
		_update_time,_update_flag
		) VALUES (
		#v_dic_pid,
		#v_level,
		'#v_label',
		#v_value,
		'#v_note',
        ##now, 1);

	-- end
-- end

-- set
SELECT eox_dic.* FROM eox_dic WHERE f_dic_id = #v_dic_id;
-- end