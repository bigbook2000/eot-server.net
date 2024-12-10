
---- 如果data实时数据表未匹配设备表，则添加一条记录
---- 这样避免每次检查实时数据表是否存在记录
---- 每次收到数据仅调用update操作

-- set
insert into n_data_rt(f_device_id, f_status, f_dtime, f_st, f_qn, f_datatime, f_data, f_dflag,
    A01,A02,A03,A04,A05,
    A06,A07,A08,A09,A10,
    A11,A12,A13,A14,A15,
    A16,A17,A18,A19,A20)
    select n_device.f_device_id, 0, ##now, 0, '1970-01-01 00:00:00', '1970-01-01 00:00:00', '', 0,
    0.0,0.0,0.0,0.0,0.0,
    0.0,0.0,0.0,0.0,0.0,
    0.0,0.0,0.0,0.0,0.0,
    0.0,0.0,0.0,0.0,0.0 
    from n_device left join n_data_rt
    on (n_device.f_device_id = n_data_rt.f_device_id) 
    where n_device.f_mn in(#v_mns)
    and n_data_rt.f_data_rt_id is null;
-- end