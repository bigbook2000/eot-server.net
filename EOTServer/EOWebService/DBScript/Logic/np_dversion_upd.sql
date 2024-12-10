
-- iff > 0 #v_version_id
	-- set
	update n_dversion set
		f_dversion = '#v_dversion',
		f_note='#v_note',
		f_dtime = ##now,
		_update_time = ##now
		where f_version_id = #v_version_id;
	-- end
-- end

-- iff <= 0 #v_version_id
	-- inc #v_version_id
    insert into n_dversion(
		f_type_id,
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
        #v_type_id,
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
    where f_version_id = #v_version_id
    and n_dversion.f_type_id = n_dtype.f_type_id;
-- end