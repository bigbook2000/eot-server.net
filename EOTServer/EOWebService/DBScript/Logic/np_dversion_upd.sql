
-- iff > 0 #v_dversion_id
	-- set
	update n_dversion set
		f_dversion = '#v_dversion',
		f_note='#v_note',
		f_dtime = ##now,
		_update_time = ##now
		where f_dversion_id = #v_dversion_id;
	-- end
-- end

-- iff <= 0 #v_dversion_id
	-- inc #v_dversion_id
    insert into n_dversion(
		f_dtype_id,
		f_dversion,
		f_sign,
        f_enable,
		f_config_data,
		f_note,
		f_ctime,
		f_dtime,
		_update_time,
		_update_flag)
		values(
        #v_dtype_id,
		'#v_dversion',
		'',
        1,
        '',
		'#v_note',
        ##now,
        ##now,
		##now, 1);
	-- end
-- end
    
-- set
select 0 as _d, '' as _s, n_dversion.*, n_dtype.f_dtype
	from n_dversion, n_dtype 
    where f_dversion_id = #v_dversion_id
    and n_dversion.f_dtype_id = n_dtype.f_dtype_id;
-- end