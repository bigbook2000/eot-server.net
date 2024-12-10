
-- set

insert into n_data_rt_cache(f_device_id, f_status, f_dtime,
    f_st, f_qn, f_datatime, f_data, f_dflag,
    A01,A02,A03,A04,A05,
    A06,A07,A08,A09,A10,
    A11,A12,A13,A14,A15,
    A16,A17,A18,A19,A20) values

    -- add for ',' #v_pack
    (#v_device_id, 1 , ##now,
    '#v_st', '#v_qn', '#v_datatime', '#v_data_json', 0,
    0.0,0.0,0.0,0.0,0.0,
    0.0,0.0,0.0,0.0,0.0,
    0.0,0.0,0.0,0.0,0.0,
    0.0,0.0,0.0,0.0,0.0)
    -- end

-- end
