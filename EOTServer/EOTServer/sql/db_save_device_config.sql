
-- set

---- 先更新设备信息
update n_device set f_dkey='#v_dkey', f_dtype='#v_dtype', f_dversion='#v_dversion' 
    where f_device_id=#v_device_id;

delete from n_config where f_device_id=#v_device_id;

---- 再存储配置信息
insert into n_config(f_device_id, f_config_data, f_ctime, _update_time, _update_flag)
    values(#v_device_id, '#v_config_data', ##now, ##now, 1);
      

-- end