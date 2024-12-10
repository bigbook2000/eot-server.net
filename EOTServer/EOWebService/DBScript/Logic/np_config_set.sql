
-- var
select f_device_id as t_device_id, f_version_id as t_version_id from n_device WHERE n_device.f_dkey = #v_dkey;
-- end
    
---- 保持配置数据

-- var
select f_config_id as t_config_id from n_config where f_device_id = #t_device_id;
-- end

-- iff > 0 #t_config_id
	-- set
	update n_config set f_config_data = '#v_config_data', f_ctime = ##now where f_device_id = #t_device_id;
	-- end
-- end
-- iff <= 0 #t_config_id
	-- set
	insert into n_config(f_device_id, f_config_data, f_ctime) values(#t_device_id, '#v_config_data', ##now);
	-- end
-- end

---- 记录历史
-- set
insert into n_config_his(f_device_id, f_version_id, f_config_data, f_ctime) 
	values (#t_device_id, #t_version_id, '#v_config_data', ##now);
-- end
        
-- set
select 0 as _d, '' as _s, #v_dkey as f_dkey, #t_device_id as f_device_id, #t_version_id as f_version_id;
-- end