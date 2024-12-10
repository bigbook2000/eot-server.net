
-- iff > 0 #v_data_field_id
    -- set
	update n_data_field set
		f_dept_id = #v_dept_id,
        f_type = '#v_type',
		f_dname = '#v_dname',
		f_kname = '#v_kname',
		f_label = '#v_label',
		f_precision = #v_precision,
		f_unit = '#v_unit',
		f_order = #v_order,
		f_width = #v_width,
		f_visible = #v_visible,
        f_note = '#v_note',
		_update_time = ##now
		where f_data_field_id = #v_data_field_id;
	-- end
-- end
    
-- iff <= 0 #v_data_field_id
	-- inc #v_data_field_id
	insert into n_data_field(
		f_dept_id,
        f_type,
		f_dname,
		f_kname,
		f_label,
		f_precision,
		f_unit,
		f_order,
		f_width,
		f_visible,
        f_note,
		_update_time,
		_update_flag)
        values (
        #v_dept_id,
        '#v_type',
		'#v_dname',
		'#v_kname',
		'#v_label',
		#v_precision,
		'#v_unit',
		#v_order,
		#v_width,
		#v_visible,
        '#v_note',
		##now,
		1);
	-- end
-- end

-- set
select * from n_data_field where f_data_field_id=#v_data_field_id;
-- end