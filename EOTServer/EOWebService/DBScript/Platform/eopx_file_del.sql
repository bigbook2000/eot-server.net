
---- type+keyid为关键字

-- set
update eox_file set _update_flag=-1 where f_type = '#v_type' and f_keyid = #v_keyid;
-- end

-- set    
select 0 as _d, '' as _s, '#v_type' as f_type, #v_keyid as f_keyid;
-- end