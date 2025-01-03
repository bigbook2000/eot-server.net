---- 单独处理版本配置

-- set
update n_dversion set
	f_config_data = '#v_config_data',
	f_dtime = ##now,
	_update_time = ##now
	where f_dversion_id = #v_dversion_id;
-- end

-- set
select n_dversion.*, n_dtype.f_dtype
	from n_dversion, n_dtype 
    where n_dversion.f_dversion_id = #v_dversion_id
    and n_dversion.f_dtype_id = n_dtype.f_dtype_id;
-- end