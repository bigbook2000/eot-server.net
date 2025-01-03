
-- var
select f_dversion_id as t_dversion_id from n_dtype,n_dversion
    where n_dtype.f_dtype_id=n_dversion.f_dtype_id
    and n_dtype.f_dtype='#v_dtype' and n_dversion.f_dversion='#v_dversion';
-- end

-- iff > 0 #t_dversion_id
    -- set

    ---- 先更新设备信息
    update n_device set f_dkey='#v_dkey', f_dversion_id=#t_dversion_id
        where f_device_id=#v_device_id;

    delete from n_config where f_device_id=#v_device_id;

    ---- 再存储配置信息
    insert into n_config(f_device_id, f_dversion_id, f_config_data, f_ctime, _update_time, _update_flag)
        values(#v_device_id, #t_dversion_id, '#v_config_data', ##now, ##now, 1);
      

    -- end
-- end