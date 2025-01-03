---- 根据mn读取设备配置

-- set
select f_config_data from n_config, n_device 
	where n_config.f_device_id=n_device.f_device_id and n_device.f_mn='#v_mn';
-- end