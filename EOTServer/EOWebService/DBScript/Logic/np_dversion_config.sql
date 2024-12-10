---- 单独处理版本配置

-- set
update n_dversion set
	f_config_data = '#v_config_data',
	f_dtime = ##now,
	_update_time = ##now
	where f_version_id = #v_version_id;
-- end

-- set
select n_dversion.*, n_dtype.f_dtype
	from n_dversion, n_dtype 
    where n_dversion.f_version_id = #v_version_id
    and n_dversion.f_type_id = n_dtype.f_type_id;
-- end