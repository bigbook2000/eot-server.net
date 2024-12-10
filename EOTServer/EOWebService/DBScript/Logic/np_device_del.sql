	
	-- set
	update n_device set _update_flag=-1 where f_device_id = #v_device_id;
	-- end

	-- set
    select 0 as _d, '' as _s, #v_device_id as f_device_id;
	-- end