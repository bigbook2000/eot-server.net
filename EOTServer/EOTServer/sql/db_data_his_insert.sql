
-- set
insert into #v_table(f_device_id, f_dtime, f_st, f_qn, f_datatime, f_data, f_dflag) values

    -- add for ',' #v_pack
    (#v_device_id, ##now,'#v_st', '#v_qn', '#v_datatime', '#v_data_json', 0)
    -- end

-- end