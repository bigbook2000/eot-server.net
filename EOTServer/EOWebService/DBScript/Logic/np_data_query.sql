
---- 根据设备ID查询指定的历史数据
---- v_type对应表名
    
-- set
select * from n_data_#v_type
	where f_device_id = #v_device_id
    and f_dtime >= '#v_start_time' and f_dtime < '#v_end_time'
    #v_order_by
-- end