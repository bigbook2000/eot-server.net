
---- 返回配置参数
-- set
select n_device.f_device_id, n_config.f_config_data 
    from n_device, n_config  
    where n_config.f_device_id = #v_device_id 
    and n_device.f_device_id = n_config.f_device_id;
-- end
