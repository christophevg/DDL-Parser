TARGET      = main
BUILD       = .build
ifdef DEBUG
CFLAGS     += -define:DEBUG
endif
CC          = mcs
RUN         = mono
ARGUMENTS  ?=
NUNIT       = nunit-console -nologo

all: run

$(BUILD)/%.exe: src/%.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $<

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(ARGUMENTS)

$(BUILD)/%.dll: test/%.cs src/*.cs
	@mkdir -p $(BUILD)
	@$(CC) -t:library -r:nunit.framework.dll -out:$@ $^

test: $(BUILD)/$(TARGET).dll
	@$(NUNIT) $<

clean:
	@rm -rf $(BUILD) TestResult.xml
