
-- set
update n_data_field set _update_flag=-1 where f_data_field_id=#v_data_field_id;
-- end

-- set
select 0 AS _d, '' AS _s, #v_data_field_id AS f_data_field_id;
-- end